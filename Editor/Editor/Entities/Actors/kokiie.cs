using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindEditor.ViewModel;

namespace WindEditor
{
	public partial class kokiie
	{
		public override void PostLoad()
		{
			base.PostLoad();
			m_actorMeshes = WResourceManager.LoadActorResource("Giant Hanging Flower");
		}

		public override void PreSave()
		{

		}
	}
}
