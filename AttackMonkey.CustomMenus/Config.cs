using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Web;
using umbraco.interfaces;
using System.Collections;

namespace AttackMonkey.CustomMenus
{
	class Config
	{
		//private backing fields
		private List<ConfigItem> _configEntries;
		private List<IAction> _allActions;
		private static Config _instance;
		private bool _ignoreForAdmin;
		private bool _useInMediaSection;

		//public properties
		public List<ConfigItem> ConfigEntries
		{
			get
			{
				return this._configEntries;
			}
		}

		public bool IgnoreForAdmin
		{
			get
			{
				return this._ignoreForAdmin;
			}
		}

		public bool UseInMediaSection
		{
			get
			{
				return this._useInMediaSection;
			}
		}

		public static Config Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = new Config();
				}
				return _instance;
			}
		}

		/// <summary>
		/// Createes the Config option, initialises collections and loads config information from the file
		/// </summary>
		private Config()
		{
			//initial map of actions
			IAction[] tmp = umbraco.BusinessLogic.Actions.Action.GetAll().ToArray(typeof(IAction)) as IAction[];

			_allActions = tmp.ToList();

			//setup config object
			_configEntries = new List<ConfigItem>();

			LoadXmlConfig();
		}

		/// <summary>
		/// Loads the content from the config file
		/// </summary>
		private void LoadXmlConfig()
		{
			//load document
			XmlDocument document = new XmlDocument();
			document.Load(HttpContext.Current.Server.MapPath("~/config/customMenus.config"));

			//add whether to ignore for members of the Administrators group
			XmlNode temp = document.SelectSingleNode("/customMenus/ignoreForAdmin");
			_ignoreForAdmin = false;
			if (temp != null)
			{
				if (!string.IsNullOrEmpty(temp.InnerText))
				{
					_ignoreForAdmin = Convert.ToBoolean(temp.InnerText);
				}
			}

			//add whether to aply rules to the media section or not
			temp = document.SelectSingleNode("/customMenus/useInMediaSection");
			_useInMediaSection = false;
			if (temp != null)
			{
				if (!string.IsNullOrEmpty(temp.InnerText))
				{
					_useInMediaSection = Convert.ToBoolean(temp.InnerText);
				}
			}
			
			//loop through each config item and set it up
			foreach (XmlNode node in document.SelectNodes("/customMenus/menuRules/add"))
			{
				if (node.NodeType != XmlNodeType.Element)
				{
					continue;
				}

				ConfigItem item = new ConfigItem();
				
				item.DocTypeAlias = node.Attributes["docTypeAlias"].Value;

				if (string.IsNullOrEmpty(item.DocTypeAlias) || (!string.IsNullOrEmpty(item.DocTypeAlias) && (item.DocTypeAlias == "content" || item.DocTypeAlias == "media")))
				{
					item.NodeId = Convert.ToInt32(node.Attributes["nodeId"].Value);
				}
				
				if (node.Attributes["clickAction"] != null)
				{
					if (!string.IsNullOrEmpty(node.Attributes["clickAction"].Value))
					{
						item.ClickAction = node.Attributes["clickAction"].Value;
					}
				}

				if (node.Attributes["menuItems"] != null)
				{
					if (!string.IsNullOrEmpty(node.Attributes["menuItems"].Value))
					{
						//split out the list of desired actions
						var menuItems = node.Attributes["menuItems"].Value.Split(',').Select(o => o.Trim());

						//loop through and add
						foreach (var actionAlias in menuItems)
						{
							if (actionAlias == "separator")
							{
								//if it's the context separator, add manually, as there's no alias on that item
								item.MenuItems.Add(umbraco.BusinessLogic.Actions.ContextMenuSeperator.Instance);
							}
							else if (!string.IsNullOrEmpty(actionAlias))
							{
								if (_allActions.Any(o => o.Alias == actionAlias))
								{
								    //if its in the list of all available actions, add it to the menu list
									item.MenuItems.Add(_allActions.First(o => o.Alias == actionAlias && o.JsFunctionName.Contains("RelationType") == false));
								}
							}
						}
					}
				}
				
				if (node.Attributes["removeMenuItems"] != null)
				{
					if (!string.IsNullOrEmpty(node.Attributes["removeMenuItems"].Value))
					{
						//split out the list of desired actions
						var menuItems = node.Attributes["removeMenuItems"].Value.Split(',').Select(o => o.Trim());

						////loop through and add
						foreach (var actionAlias in menuItems)
						{
							if (actionAlias == "separator")
							{
								//if it's the context separator, add manually, as there's no alias on that item
								item.RemoveMenuItems.Add(umbraco.BusinessLogic.Actions.ContextMenuSeperator.Instance);
							}
							else if (!string.IsNullOrEmpty(actionAlias))
							{
								//add to the list of things to remove
								item.RemoveMenuItems.Add(_allActions.First(o => o.Alias == actionAlias));
							}
						}
					}
				}

				//only add the config item if it hasn't already been added
				if (!(_configEntries.Any(o => o.DocTypeAlias == item.DocTypeAlias) &! string.IsNullOrEmpty(item.DocTypeAlias)))
				{
					_configEntries.Add(item);
				}
				else if (!(_configEntries.Any(o => o.NodeId == item.NodeId) && item.NodeId != 0))
				{
					_configEntries.Add(item);
				}
			}
		}

		/// <summary>
		/// Clears the singleton, so that it's refreshed next time someone asks for it
		/// </summary>
		public static void RefreshInstance()
		{
			_instance = null;
		}
	}
}