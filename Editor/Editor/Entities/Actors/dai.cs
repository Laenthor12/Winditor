using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindEditor.ViewModel;

namespace WindEditor
{
	public partial class dai
	{
		public override void PostLoad()
		{
            m_actorMeshes = WResourceManager.LoadActorResource("Pedestal");
            base.PostLoad();
		}

		public override void PreSave()
		{

		}
	}
}
