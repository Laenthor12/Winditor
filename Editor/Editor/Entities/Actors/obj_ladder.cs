using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindEditor.ViewModel;

namespace WindEditor
{
	public partial class obj_ladder
	{
		public override void PostLoad()
		{
            UpdateModel();
            base.PostLoad();
		}

		public override void PreSave()
		{

		}

        private void UpdateModel()
        {
            m_actorMeshes.Clear();
            m_objRender = null;
            switch (Unknown_1)
            {
                case 0:
                    m_actorMeshes = WResourceManager.LoadActorResource("Ladder 0");
                    break;
                case 1:
                    m_actorMeshes = WResourceManager.LoadActorResource("Ladder 1");
                    break;
                case 2:
                    m_actorMeshes = WResourceManager.LoadActorResource("Ladder 2");
                    break;
                case 3:
                    m_actorMeshes = WResourceManager.LoadActorResource("Ladder 3");
                    break;
                case 4:
                    m_actorMeshes = WResourceManager.LoadActorResource("Ladder 4");
                    break;
                default:
                    m_objRender = WResourceManager.LoadObjResource("resources/editor/EditorCube.obj", new OpenTK.Vector4(1f, 1f, 1f, 1f));
                    break;
            }
        }
	}
}
