using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;

namespace GestureGis2
{
    internal class Button1 : Button
    {
        protected override void OnClick()
        {
            
            var pane = FrameworkApplication.DockPaneManager.Find("GestureGis2_Dockpane1");

            // determine visibility
            bool visible = pane.IsVisible;

            // activate it
            pane.Activate();

            // determine dockpane state
            DockPaneState state = pane.DockState;

            // pin it
            pane.Pin();

            // hide it
            // pane.Hide();
        }
    }
}
