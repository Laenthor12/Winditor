using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindEditor.ViewModel;

namespace WindEditor
{
	public partial class dk
	{
		public override void PostLoad()
		{
			m_actorMeshes = WResourceManager.LoadActorResource("Helmaroc King");
			base.PostLoad();
		}

		public override void PreSave()
		{

		}
	}
}
