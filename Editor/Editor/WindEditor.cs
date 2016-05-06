﻿using OpenTK.Graphics.OpenGL;
using System.Collections.Generic;

namespace Editor
{
    class WindEditor
    {
        public WWorld MainWorld { get { return m_editorWorlds[0]; } }

        private List<WWorld> m_editorWorlds = new List<WWorld>();

        public WindEditor()
        {
            WWorld baseWorld = new WWorld();
            m_editorWorlds.Add(baseWorld);

            //baseWorld.LoadMap(@"E:\New_Data_Drive\WindwakerModding\De-Arc-ed Stage\MiniHyo\");
        }

        internal void OnViewportResized(int width, int height)
        {
            foreach(WWorld world in m_editorWorlds)
            {
                world.OnViewportResized(width, height);
            }
        }

        public void OnApplicationShutdown()
        {
            foreach (WWorld world in m_editorWorlds)
                world.UnloadMap();
        }

        public void ProcessTick()
        {
            foreach (WWorld world in m_editorWorlds)
                world.ProcessTick();

            GL.Flush();
        }
    }
}
