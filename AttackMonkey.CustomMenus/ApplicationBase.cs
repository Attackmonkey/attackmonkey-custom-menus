using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.IO;
using umbraco;
using umbraco.cms.presentation.Trees;
using umbraco.cms.businesslogic.web;
using umbraco.cms.businesslogic.media;
using umbraco.BasePages;
using umbraco.interfaces;

namespace AttackMonkey.CustomMenus
{
	public class ApplicationBase : umbraco.BusinessLogic.ApplicationBase
	{
		/// <summary>
		/// Main methods, sets up config watcher and event listeners
		/// </summary>
		public ApplicationBase()
		{
			//set up action listeners
			BaseTree.BeforeNodeRender += new umbraco.cms.presentation.Trees.BaseTree.BeforeNodeRenderEventHandler(SetCustomMenus);
			
			//code to expire config if the config file is changed
			if (HttpContext.Current.Application["customMenus"] == null)
			{
				string path = HttpContext.Current.Server.MapPath("~/config/");
				HttpContext.Current.Application.Add("customMenus", new FileSystemWatcher(path));
				FileSystemWatcher watcher = (FileSystemWatcher)HttpContext.Current.Application["customMenus"];
				watcher.EnableRaisingEvents = true;
				watcher.IncludeSubdirectories = true;
				watcher.Changed += new FileSystemEventHandler(this.expireConfig);
				watcher.Created += new FileSystemEventHandler(this.expireConfig);
				watcher.Deleted += new FileSystemEventHandler(this.expireConfig);
			}
		}

		/// <summary>
		/// Expires the config for the package
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected void expireConfig(object sender, FileSystemEventArgs e)
		{
			Config.RefreshInstance();
		}

		/// <summary>
		/// New static method to enable it to work properly with CustomContentTree
		/// </summary>
		/// <param name="node">the tree node to process</param>
		public static void ProcessMenu(ref umbraco.cms.presentation.Trees.XmlTreeNode node)
		{
			//if they've set the ignore for Admins option, don't parse these rules
			if (!(Config.Instance.IgnoreForAdmin && umbraco.BusinessLogic.User.GetCurrent().IsAdmin()))
			{
				//only call if the node has a menu in the first place
				if (node.Menu != null && (node.NodeType == "content" || node.NodeType == "media"))
				{
					string alias = string.Empty;
					int nodeId = 0;
					string path = string.Empty;

					ConfigItem config = null;

					if (node.NodeType == "content")
					{
						//get the document
						Document doc = new Document(true, int.Parse(node.NodeID));

						alias = doc.ContentType.Alias;
						nodeId = Convert.ToInt32(node.NodeID);
						path = doc.Path;
					}
					else if (node.NodeType == "media" && Config.Instance.UseInMediaSection)
					{
						//get the media item
						Media media = new Media(int.Parse(node.NodeID));

						alias = media.ContentType.Alias;
						nodeId = Convert.ToInt32(node.NodeID);
						path = media.Path;
					}

					//only carry on if we have a node id greater than 0
					if (nodeId != 0)
					{
						if (Config.Instance.ConfigEntries.Any(o => o.DocTypeAlias == alias))
						{
							//doctype match, get the config item
							config = Config.Instance.ConfigEntries.First(o => o.DocTypeAlias == alias);
						}
						else if (Config.Instance.ConfigEntries.Any(o => o.NodeId == nodeId))
						{
							//node id match, get the config item
							config = Config.Instance.ConfigEntries.First(o => o.NodeId == nodeId);
						}

						if (config != null)
						{
							//first clear the current menu items, but only if there are things we can do with it
							if (config.MenuItems.Count > 0)
							{
								node.Menu.Clear();
							}

							//if there's a click action, add it
							if (!string.IsNullOrEmpty(config.ClickAction))
							{
								node.Action = config.ClickAction;
							}

							//get the list of actions the user is allowed to do on this node.
							List<IAction> allowedActions = umbraco.BusinessLogic.Actions.Action.FromString(UmbracoEnsuredPage.CurrentUser.GetPermissions(path));

							//now loop through and add the items from the list, checking that the current user has permissions for the item in the first place
							foreach (var action in config.MenuItems)
							{
								if (!action.CanBePermissionAssigned || (action.CanBePermissionAssigned && allowedActions.Contains(action)))
								{
									node.Menu.Add(action);
								}
							}

							//loop through the remove menu items and remove them
							foreach (var action in config.RemoveMenuItems)
							{
								if (node.Menu.Any(a => a.Alias == action.Alias))
								{
									node.Menu.Remove(node.Menu.First(a => a.Alias == action.Alias && a.JsFunctionName.Contains("RelationType") == false));
								}
							}

							//experimental code for fixing URL issue
							
						}
					}
				}
			}
		}

		/// <summary>
		/// Sets the custom menus, based on your config
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="node"></param>
		/// <param name="e"></param>
		void SetCustomMenus(ref umbraco.cms.presentation.Trees.XmlTree sender, ref umbraco.cms.presentation.Trees.XmlTreeNode node, EventArgs e)
		{
			ProcessMenu(ref node);
		}
	}
}