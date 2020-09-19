﻿#region ================== Copyright (c) 2020 Boris Iwanski

/*
 * This program is free software: you can redistribute it and/or modify
 *
 * it under the terms of the GNU General Public License as published by
 * 
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 *    but WITHOUT ANY WARRANTY; without even the implied warranty of
 * 
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
 * 
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.If not, see<http://www.gnu.org/licenses/>.
 */

#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CodeImp.DoomBuilder.UDBScript
{
	public partial class QueryParametersForm : Form
	{
		private Stopwatch stopwatch;

		public QueryParametersForm(Stopwatch stopwatch)
		{
			InitializeComponent();

			this.stopwatch = stopwatch;
		}

		public void AddParameter(string name, string description, int type, object defaultvalue)
		{
			int index = parametersview.ParametersView.Rows.Add();

			ScriptOption so = new ScriptOption(name, description, type, defaultvalue);

			parametersview.ParametersView.Rows[index].Cells["Description"].Value = description;
			parametersview.ParametersView.Rows[index].Cells["Value"].Value = so.value;
			parametersview.ParametersView.Rows[index].Tag = so;
		}

		public void Clear()
		{
			parametersview.ParametersView.Rows.Clear();
		}

		public ExpandoObject GetScriptOptions()
		{
			return parametersview.GetScriptOptions();
		}

		private void btnOK_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
			Close();
		}

		private void btnCancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}
	}
}
