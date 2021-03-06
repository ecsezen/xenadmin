﻿/* Copyright (c) Citrix Systems Inc. 
 * All rights reserved. 
 * 
 * Redistribution and use in source and binary forms, 
 * with or without modification, are permitted provided 
 * that the following conditions are met: 
 * 
 * *   Redistributions of source code must retain the above 
 *     copyright notice, this list of conditions and the 
 *     following disclaimer. 
 * *   Redistributions in binary form must reproduce the above 
 *     copyright notice, this list of conditions and the 
 *     following disclaimer in the documentation and/or other 
 *     materials provided with the distribution. 
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND 
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, 
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF 
 * MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE 
 * DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR 
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, 
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, 
 * BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR 
 * SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS 
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, 
 * WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING 
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE 
 * OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF 
 * SUCH DAMAGE.
 */

using System;
using System.Windows.Forms;
using XenAdmin.Core;
using System.Drawing;
using System.Linq;
using XenAPI;

namespace XenAdmin.Controls
{
    public class VgpuComboBox : NonSelectableComboBox
    {
        public VgpuComboBox()
        {
            DrawMode = DrawMode.OwnerDrawVariable;
            DropDownStyle = ComboBoxStyle.DropDownList;
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index > -1)
            {
                e.DrawBackground();

                GpuTuple obj = Items[e.Index] as GpuTuple;
                if (obj == null)
                    return;

                if (IsHeaderItem(obj))
                {
                    Drawing.DrawText(e.Graphics, obj.ToString(), Program.DefaultFontBold,
                                     e.Bounds, SystemColors.ControlText,
                                     TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                }
                else if (obj.IsNotEnabledVgpu)
                {
                    string text = (obj.IsVgpuSubitem ? "    " : string.Empty) + obj;

                    Drawing.DrawText(e.Graphics, text, Program.DefaultFont,
                                     e.Bounds, Color.DarkGray,
                                     TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                }
                else
                {
                    var textColor = e.ForeColor;

                    if ((e.State & DrawItemState.Disabled) != 0)
                        textColor = SystemColors.GrayText;

                    string indentedText = obj.ToString();
                    if (obj.IsVgpuSubitem)
                        indentedText = "    " + obj;

                    Drawing.DrawText(e.Graphics, indentedText, Program.DefaultFont,
                                     e.Bounds, textColor,
                                     TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

                    e.DrawFocusRectangle();
                }
            }

            base.OnDrawItem(e);
        }

        protected bool IsHeaderItem(object obj)
        {
            var tuple = obj as GpuTuple;
            return tuple != null && tuple.IsGpuHeaderItem;
        }

        protected override bool IsItemNonSelectable(object obj)
        {
            var tuple = obj as GpuTuple;
            return tuple != null && (tuple.IsGpuHeaderItem || tuple.IsNotEnabledVgpu);
        }

    }

    internal class GpuTuple : IEquatable<GpuTuple>
    {
        public readonly GPU_group GpuGroup;
        public readonly VGPU_type[] VgpuTypes;
        public readonly bool IsGpuHeaderItem;
        public readonly bool IsVgpuSubitem;
        public readonly bool IsFractionalVgpu;
        public readonly bool IsNotEnabledVgpu;
        private readonly string displayName = string.Empty;

        public GpuTuple(GPU_group gpuGroup, VGPU_type[] vgpuTypes, VGPU_type[] disabledVGpuTypes)
        {
            GpuGroup = gpuGroup;
            VgpuTypes = vgpuTypes;

            if (GpuGroup == null)
            {
                //this refers to the item "None"
                displayName = Messages.GPU_NONE;
            }
            else if (VgpuTypes == null  || VgpuTypes.Length == 0 || vgpuTypes[0] == null)
            {
                //this refers to an item mapping a GPU with only passthrough type
                displayName = GpuGroup.Name;
            }
            else if (VgpuTypes.Length == 1)
            {
                //this refers to vGPU type which is a subitem of a GPU group
                displayName = VgpuTypes[0].Description;
                IsVgpuSubitem = true;
                IsFractionalVgpu = VgpuTypes[0].max_heads != 0;
                if (disabledVGpuTypes != null && disabledVGpuTypes.Select(t => t.opaque_ref).Contains(VgpuTypes[0].opaque_ref))
                    IsNotEnabledVgpu = true;
            }
            else
            {
                //this refers to a GPU group head item, which is also non-selectable
                displayName = GpuGroup.Name;
                IsGpuHeaderItem = true;
            }
        }

        public bool Equals(GpuTuple other)
        {
            if (GpuGroup == null && other.GpuGroup == null)
                return true;
            if (GpuGroup == null || other.GpuGroup == null)
                return false;

            bool result = GpuGroup.Equals(other.GpuGroup);

            if (result)
            {
                if ((VgpuTypes == null || VgpuTypes.Length == 0) &&
                    (other.VgpuTypes == null || other.VgpuTypes.Length == 0))
                    return true;

                if ((VgpuTypes == null || VgpuTypes.Length == 0) ||
                    (other.VgpuTypes == null || other.VgpuTypes.Length == 0))
                    return false;

                if (VgpuTypes.Length != other.VgpuTypes.Length)
                    return false;

                for (int i = 0; i < VgpuTypes.Length; i++)
                {
                    if (!VgpuTypes[i].Equals(other.VgpuTypes[i]))
                        return false;
                }
            }

            return result;
        }

        public override string ToString()
        {
            return displayName;
        }
    }
}
