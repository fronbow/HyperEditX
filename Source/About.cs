using System;
using UnityEngine;

namespace HyperEditX.About {
  public static class Panel {
    public static Action Create() {
      return () => UiHelper.Window.Create("About", true, true, 500, 200, w => GUILayout.Label(AboutContents));
    }

    private const string AboutContents = @"For support and contact information, please visit: http://www.kerbaltek.com/

This is a highly eccentric plugin, so there may be lots of bugs and explosions - please tell us if you find any.

Created by:
khyperia (original creator, code)
Ezriilc (web, code)
sirkut (code)
payo (code [Planet Editor])
forecaster (graphics, logo)

GPL license.";
  }
}