using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using umbraco.interfaces;

namespace AttackMonkey.CustomMenus
{
	/// <summary>
	/// ConfigItem class, used to store the settings from the config file
	/// </summary>
	class ConfigItem
	{
		public string DocTypeAlias { get; set; }
		public int NodeId { get; set; }
		public string ClickAction { get; set; }
		public List<IAction> MenuItems { get; set; }
		public List<IAction> RemoveMenuItems { get; set; }

		public ConfigItem()
		{
			MenuItems = new List<IAction>();
			RemoveMenuItems = new List<IAction>();
		}
	}
}