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

        // personal constants
        public const int UIOffset = 0;

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
            int gizmoCount = 0;
            for (int i = 0; i < gizmoGroups.Count; i++)
            {
                if (gizmoGroups[i][0] != null && gizmoGroups[i][0].Visible)
                    gizmoCount++;
            }
            return gizmoCount;
        }

        // patch to begining of gizmo drawing
        public static void Prefix(IEnumerable<Gizmo> gizmos, float startX)
        {
            CacheGizmos(gizmos);

            // calculate viewHeight
            int gizmoCount = GetGizmoCount();
            float gizmoSpacing = GizmoGridDrawer.GizmoSpacing.x;

            float widthTracker = 0;
            int rowCount = 1;
            for (int i = 0; i < gizmoCount; i++)
            {
                // get width of gizmo
                float gizmoWidth = gizmoGroups[i][0].GetWidth(float.MaxValue) + gizmoSpacing;
                widthTracker += gizmoWidth;

                if (widthTracker > (UI.screenWidth - startX - (sideOffset - gizmoSpacing)))
                {
                    rowCount++;
                    widthTracker = gizmoWidth;
                }
            }

            // calulate rect heights for scroll view
            float viewHeight = rowCount * (buttonSize + gizmoSpacing);
            float outHeight = Mathf.Min(viewHeight, GizmoSettings.outHeight);

            // add 10 so top of gizmos are not cut off so it looks nice
            viewHeight += 10f;
            outHeight += 10f;

            // rendering hack
            UI.screenHeight += UIOffset;

            int screenHeight = UI.screenHeight;
            int screenWidth = UI.screenWidth;

            // create rects
            Rect gizmoOut = new Rect(startX, screenHeight - outHeight - bottomOffset - UIOffset, screenWidth - sideOffset - startX, outHeight);
            Rect gizmoView = new Rect(startX, screenHeight - viewHeight, screenWidth - sideOffset - scrollBarOffset - startX, viewHeight);
            //Rect gizmoGroup = new Rect(0f, bottomOffset, screenWidth - scrollBarOffset - sideOffset, screenHeight);
            Rect gizmoGroup = new Rect(0f, bottomOffset, screenWidth, screenHeight);

            // start scroll
            Widgets.BeginScrollView(gizmoOut, ref scroll, gizmoView, GizmoSettings.showScrollBar);
            Widgets.BeginGroup(gizmoGroup);
        }

        // patch to end of gizmo drawing
        public static void Postfix()
        {
            Widgets.EndGroup();
            Widgets.EndScrollView();
            UI.screenHeight -= UIOffset;
        }
    }
}
