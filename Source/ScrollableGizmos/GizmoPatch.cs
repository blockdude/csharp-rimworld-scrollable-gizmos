using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using Verse.Steam;
using UnityEngine;
using HarmonyLib;

namespace ScrollableGizmos
{
    [StaticConstructorOnStartup]
    [HarmonyPatch(typeof(GizmoGridDrawer), nameof(GizmoGridDrawer.DrawGizmoGrid))]
    class GizmoPatch
    {
        public static Harmony harmony;
        public static bool patched;
        static GizmoPatch()
        {
            harmony = new Harmony("Blockdude.ScrollableGizmos");

            if (GizmoSettings.enabled)
            {
                harmony.PatchAll();
                patched = true;
            }
            else
            {
                patched = false;
            }
        }

        // gizmo scroll
        public static Vector2 scroll = new Vector2();

        // some constants found in gizmo code
        public const float scrollBarOffset = 16f;
        public const float sideOffset = 147f;
        public const float bottomOffset = 35f;
        public const float buttonSize = 75f;

        // lists for gizmo count
        public static List<Gizmo> tmpAllGizmos = new List<Gizmo>();
        public static List<List<Gizmo>> gizmoGroups = new List<List<Gizmo>>();

        public static void CacheGizmos(IEnumerable<Gizmo> gizmos)
        {
            /*
             * Begining of copied code so i can count gizmos
             */
            tmpAllGizmos.Clear();
			gizmoGroups.Clear();
			tmpAllGizmos.AddRange(gizmos);
			for (int i = 0; i < tmpAllGizmos.Count; i++)
			{
				Gizmo gizmo = tmpAllGizmos[i];
				bool flag = false;
				for (int j = 0; j < gizmoGroups.Count; j++)
				{
					if (gizmoGroups[j][0].GroupsWith(gizmo))
					{
						flag = true;
						gizmoGroups[j].Add(gizmo);
						gizmoGroups[j][0].MergeWith(gizmo);
						break;
					}
				}
				if (!flag)
				{
					List<Gizmo> list = SimplePool<List<Gizmo>>.Get();
					list.Add(gizmo);
					gizmoGroups.Add(list);
				}
			}
            /*
             * End of copied code
             */
        }

        public static int GetGizmoCount()
        {
            // calculate viewHeight
            int gizmoCount = 0;
            for (int i = 0; i < gizmoGroups.Count; i++)
            {
                if (gizmoGroups[i][0] != null && gizmoGroups[i][0].Visible)
                    gizmoCount++;
            }
            return gizmoCount;
        }

        // patch to begining of gizmo drawing
        public static void Prefix(ref IEnumerable<Gizmo> gizmos, ref float startX)
        {
            CacheGizmos(gizmos);

            // calculate viewHeight
            int gizmoCount = GetGizmoCount();

            // height of full scroll view
            float viewHeight;

            // height of out rect for scroll view
            float outHeight;

            // total columns of gizmos
            int gizmoColumnCount;

            // total rows of gizmos
            int gizmoRowCount;

            // spacing that is set in the gizmo
            float gizmoSpacing = GizmoGridDrawer.GizmoSpacing.x;

            // calculate gizmo column count (unreliable solution for different width gizmos)
            gizmoColumnCount = 0;
            for (int i = 0; i < gizmoCount; i++)
            {
                gizmoColumnCount++;
                if ((gizmoColumnCount * (buttonSize + gizmoSpacing)) > (UI.screenWidth - startX - (sideOffset - gizmoSpacing)))
                {
                    gizmoColumnCount--;
                    break;
                }
            }

            // calculate gizmo row count
            gizmoRowCount = Mathf.CeilToInt((float)((float)gizmoCount / (float)gizmoColumnCount));

            // calulate rect heights for scroll view
            viewHeight = gizmoRowCount * (buttonSize + gizmoSpacing);
            outHeight = Mathf.Min(viewHeight, GizmoSettings.outHeight);

            // add 10 so top of gizmos are not cut off so it looks nice
            viewHeight += 10f;
            outHeight += 10f;

            // create rects
            Rect gizmoGroup = new Rect(0f, bottomOffset, Screen.width - scrollBarOffset - sideOffset, Screen.height);
            Rect gizmoOut = new Rect(0f, Screen.height - outHeight - bottomOffset, Screen.width - sideOffset, outHeight);
            Rect gizmoView = new Rect(0f, Screen.height - viewHeight, Screen.width - sideOffset - scrollBarOffset, viewHeight);

            // start scroll
            Widgets.BeginScrollView(gizmoOut, ref scroll, gizmoView, GizmoSettings.showScrollBar);
            GUI.BeginGroup(gizmoGroup);
        }

        // patch to end of gizmo drawing
        public static void Postfix()
        {
            GUI.EndGroup();
            Widgets.EndScrollView();
        }
    }
}
