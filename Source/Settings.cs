using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.UI.Screens;

namespace HyperEditX.Settings {
  

  public static class Panel {
    public static Action Create() {
      var view = View();
      return () => UiHelper.Window.Create("Settings", true, true, 200, -1, w => view.Draw());
    }

    public static UiHelper.IView View() {
      return new UiHelper.VerticalView(new UiHelper.IView[]
      {
        new UiHelper.LabelView("Settings", "Encapsulating all settings"),
      });

    }
  }

}
