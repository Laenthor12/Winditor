﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using WindEditor.Editor.Modes;
using System.Windows.Forms;

namespace WindEditor
{
    public class PlaytestManager
    {
        private Process m_DolphinInstance;
        private ProcessStartInfo m_DolphinStartInfo;

        List<string> m_BackedUpFilePaths;

        public PlaytestManager()
        {
            m_DolphinInstance = null;
        }

        public void RequestStartPlaytest(WMap map, MapLayer active_layer)
        {
            if (m_DolphinInstance != null)
            {
                m_DolphinInstance.CloseMainWindow();
                m_DolphinInstance.WaitForExit();
            }

            StartPlaytest(map, active_layer);
        }

        private void StartPlaytest(WMap map, MapLayer active_layer)
        {
            string dolphinPath = Path.Combine(WSettingsManager.GetSettings().DolphinDirectory.FilePath, "Dolphin.exe");
            if (!File.Exists(dolphinPath))
            {
                MessageBox.Show("You must specify the path to Dolphin in the options menu before you can playtest.", "Dolphin not found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Console.WriteLine($"Stage name: { map.MapName }, Room Name: { map.FocusedSceneLabel }");

            string map_path = Path.Combine(WSettingsManager.GetSettings().RootDirectoryPath, "files", "res", "stage", map.MapName);
            map.ExportToDirectory(map_path);

            List<string> filesToBackUp = new List<string> { "sys/main.dol" };
            if (WSettingsManager.GetSettings().HeapDisplay)
            {
                filesToBackUp.Add("sys/boot.bin");
            }

            m_BackedUpFilePaths = new List<string>();
            foreach (string filePath in filesToBackUp)
            {
                string fullPath = Path.Combine(WSettingsManager.GetSettings().RootDirectoryPath, filePath);
                m_BackedUpFilePaths.Add(fullPath);
            }

            string dolPath = Path.Combine(WSettingsManager.GetSettings().RootDirectoryPath, "sys", "main.dol");

            m_DolphinStartInfo = new ProcessStartInfo(dolphinPath);
            m_DolphinStartInfo.Arguments = $"-b -e \"{ dolPath }\"";

            int room_no = 0;
            int spawn_id = 0;

            GetRoomAndSpawnID(map.FocusedScene, out room_no, out spawn_id);

            if (!File.Exists(dolPath))
            {
                Console.WriteLine("ISO root has no executable!");
                return;
            }

            foreach (string filePath in m_BackedUpFilePaths)
            {
                string backupPath = filePath + ".bak";
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }
                File.Copy(filePath, backupPath);
            }

            Patch testRoomPatch = JsonConvert.DeserializeObject<Patch>(File.ReadAllText(@"resources\patches\test_room_diff.json"));
            testRoomPatch.Files[0].Patchlets.Add(new Patchlet(0x8022CFF8, new List<byte>(Encoding.ASCII.GetBytes(map.MapName))));
            testRoomPatch.Files[0].Patchlets.Add(new Patchlet(0x800531E3, new List<byte>(new byte[] { (byte)spawn_id })));
            testRoomPatch.Files[0].Patchlets.Add(new Patchlet(0x800531E7, new List<byte>(new byte[] { (byte)room_no })));
            testRoomPatch.Files[0].Patchlets.Add(new Patchlet(0x800531EB, new List<byte>(new byte[] { (byte)(active_layer - 1) })));
            testRoomPatch.Apply(WSettingsManager.GetSettings().RootDirectoryPath);

            Patch devModePatch = JsonConvert.DeserializeObject<Patch>(File.ReadAllText(@"resources\patches\developer_mode_diff.json"));
            devModePatch.Apply(WSettingsManager.GetSettings().RootDirectoryPath);

            Patch particleIdsPatch = JsonConvert.DeserializeObject<Patch>(File.ReadAllText(@"resources\patches\missing_particle_ids_diff.json"));
            particleIdsPatch.Apply(WSettingsManager.GetSettings().RootDirectoryPath);

            if (WSettingsManager.GetSettings().HeapDisplay)
            {
                Patch heapDisplayPatch = JsonConvert.DeserializeObject<Patch>(File.ReadAllText(@"resources\patches\heap_display_diff.json"));
                heapDisplayPatch.Apply(WSettingsManager.GetSettings().RootDirectoryPath);
            }

            m_DolphinInstance = Process.Start(m_DolphinStartInfo);

            m_DolphinInstance.EnableRaisingEvents = true;
            m_DolphinInstance.Exited += OnDolphinExited;
        }

        private void OnDolphinExited(object sender, EventArgs e)
        {
            m_DolphinInstance.Exited -= OnDolphinExited;
            m_DolphinInstance = null;

            foreach (string filePath in m_BackedUpFilePaths)
            {
                string backupPath = filePath + ".bak";
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                File.Copy(backupPath, filePath);
                File.Delete(backupPath);
            }
        }

        private byte GetRoomNumberFromSceneName(string name)
        {
            if (name == "Stage")
                return 0;

            byte room_no = 0;

            string room_removed = name.ToLowerInvariant().Replace("room", "");
            byte.TryParse(room_removed, out room_no);

            return room_no;
        }

        private void GetRoomAndSpawnID(WScene scene, out int room, out int spawn)
        {
            room = 0;
            spawn = 0;

            Selection<WDOMNode> selected = null;

            if (scene.World.CurrentMode is ActorMode)
            {
                ActorMode mode = scene.World.CurrentMode as ActorMode;
                selected = mode.EditorSelection;
            }

            room = GetRoomNumberFromSceneName(scene.Name);
            if (selected != null && selected.PrimarySelectedObject is SpawnPoint)
            {
                // If the user has a spawn point selected, spawn the player at that spawn point.
                SpawnPoint spawn_pt = (SpawnPoint)selected.PrimarySelectedObject;

                room = spawn_pt.Room;
                spawn = spawn_pt.SpawnID;
            }
            else if (selected != null && selected.PrimarySelectedObject != null)
            {
                // If the user has something besides a spawn point selected, spawn the player at the first spawn point in the room that the selected object is in.
                WDOMNode cur_object = selected.PrimarySelectedObject;
                while (cur_object.Parent != null)
                {
                    cur_object = cur_object.Parent;
                }
                WRoom room_node;
                if (cur_object is WRoom)
                {
                    room_node = cur_object as WRoom;
                } else
                {
                    // A stage entity is selected. Use whatever spawn point is physically closest to the selected entity, regardless of what scene that spawn is in.
                    List<SpawnPoint> allSpawnPts = new List<SpawnPoint>();
                    foreach (WScene scn in scene.World.Map.SceneList)
                    {
                        allSpawnPts.AddRange(scn.GetChildrenOfType<SpawnPoint>());
                    }

                    SpawnPoint closestSpawnPt = allSpawnPts.OrderBy(spawnPt => (spawnPt.Transform.Position - selected.PrimarySelectedObject.Transform.Position).Length).First();
                    room = closestSpawnPt.Room;
                    spawn = closestSpawnPt.SpawnID;
                    return;
                }

                SpawnPoint spawn_pt = room_node.GetChildrenOfType<SpawnPoint>().FirstOrDefault();
                if (spawn_pt != null)
                {
                    room = spawn_pt.Room;
                    spawn = spawn_pt.SpawnID;
                }
                else
                {
                    WStage stage = room_node.World.Map.SceneList.First(x => x.GetType() == typeof(WStage)) as WStage;
                    spawn_pt = stage.GetChildrenOfType<SpawnPoint>().FirstOrDefault(x => x.Room == room_node.RoomIndex);
                    if (spawn_pt != null)
                    {
                        room = spawn_pt.Room;
                        spawn = spawn_pt.SpawnID;
                    }
                }
            }
        }
    }
}
