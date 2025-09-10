// Path: Mixins/Modules/ScreenMgr/ScreenMgr.cs
// Guardrails: C# 6 / .NET Framework 4.8; VRage-first. Mixins MUST NOT inherit MyGridProgram.

using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        // Minimal, allocation-aware Screen Manager mixin
        // Enclosure rule enforced: everything inside partial Program.

        // --- Configuration (static) ---
        const int SCREEN_STACK_CAPACITY = 8; // Keep small for PB RAM
        const int BREADCRUMB_MAX_CHARS = 300; // Limit to a single LCD line typically
        const char BREADCRUMB_SEP = '›'; // ASCII-friendly arrow

        // --- State (single-allocation fields) ---
        int _screenDepth; // 0 == root
        readonly string[] _screenTitles = new string[SCREEN_STACK_CAPACITY];
        readonly int[] _screenIds = new int[SCREEN_STACK_CAPACITY];
        readonly StringBuilder _sb = new StringBuilder(512);

        // Optional: surfaces to paint breadcrumbs onto (set by PB sample)
        IMyTextSurface _crumbSurface; // Single surface; could be extended to a list

        // API: push a new screen with numeric ID and human title
        public bool ScreenPush(int id, string title)
        {
            if (_screenDepth >= SCREEN_STACK_CAPACITY) return false;
            _screenIds[_screenDepth] = id;
            _screenTitles[_screenDepth] = title; // Title references reused; callers should reuse strings if possible
            _screenDepth++;
            return true;
        }

        // API: pop the current screen (no-op returns false on underflow)
        public bool ScreenPop()
        {
            if (_screenDepth <= 0) return false;
            _screenDepth--;
            _screenTitles[_screenDepth] = null; // release reference; avoids accidental growth
            return true;
        }

        // API: clear entire stack back to root
        public void ScreenReset()
        {
            while (_screenDepth > 0)
            {
                _screenDepth--;
                _screenTitles[_screenDepth] = null;
            }
        }

        // API: set/unset the default breadcrumb surface (e.g., PB LCD 0)
        public void ScreenSetBreadcrumbSurface(IMyTextSurface surface)
        {
            _crumbSurface = surface;
        }

        // API: render breadcrumb to provided surface (or default)
        public void ScreenRenderBreadcrumb(IMyTextSurface surface = null)
        {
            var s = surface ?? _crumbSurface;
            if (s == null) return;

            _sb.Clear();
            if (_screenDepth == 0)
            {
                _sb.Append("Root");
            }
            else
            {
                // Build: Root › A › B
                _sb.Append("Root");
                for (var i = 0; i < _screenDepth; i++)
                {
                    _sb.Append(' ').Append(BREADCRUMB_SEP).Append(' ');
                    var t = _screenTitles[i];
                    if (!string.IsNullOrEmpty(t)) _sb.Append(t);
                    else _sb.Append('#').Append(_screenIds[i]);
                    if (_sb.Length > BREADCRUMB_MAX_CHARS) break;
                }
            }

            // Draw text (mono, centered vertically; left-aligned)
            s.ContentType = ContentType.TEXT_AND_IMAGE;
            s.WriteText(_sb, false);
        }

        // Optional: helper to peek current screen id
        public int ScreenCurrentId()
        {
            return _screenDepth > 0 ? _screenIds[_screenDepth - 1] : 0;
        }

        // Optional: helper to peek current screen title
        public string ScreenCurrentTitle()
        {
            return _screenDepth > 0 ? _screenTitles[_screenDepth - 1] : "Root";
        }
    }
}
