using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.IO;
using Umbraco.Core;
using umbraco.interfaces;

namespace AttackMonkey.CustomMenus
{
	public class ApplicationBase : IApplicationEventHandler
	{
		public void OnApplicationInitialized(UmbracoApplicationBase umbracoApplication, Umbraco.Core.ApplicationContext applicationContext)
		{

		}

		public void OnApplicationStarted(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
		{

		}

		public void OnApplicationStarting(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
		{
			//set up action listeners
			Umbraco.Web.Trees.ContentTreeController.MenuRendering += SetCustomMenus;
			Umbraco.Web.Trees.ContentTreeController.TreeNodesRendering += ContentTreeController_TreeNodesRendering;
			
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

		void ContentTreeController_TreeNodesRendering(Umbraco.Web.Trees.TreeControllerBase sender, Umbraco.Web.Trees.TreeNodesRenderingEventArgs e)
		{
			//if they've set the ignore for Admins option, don't parse these rules
			if (!(Config.Instance.IgnoreForAdmin && umbraco.BusinessLogic.User.GetCurrent().IsAdmin()))
			{
				foreach (var node in e.Nodes)
				{
					//only call if the node has a menu in the first place
					if (sender.TreeAlias == "content" || sender.TreeAlias == "media")
					{
						string alias = string.Empty;
						int nodeId = 0;
						string path = string.Empty;

						ConfigItem config = null;

						if (sender.TreeAlias == "content" && Convert.ToInt32(node.Id) > 0)
						{
							//get the document
							var content = sender.Services.ContentService.GetById(Convert.ToInt32(node.Id));

							alias = content.ContentType.Alias;
							nodeId = content.Id;
							path = content.Path;
						}
						else if (sender.TreeAlias == "media" && Convert.ToInt32(node.Id) > 0 && Config.Instance.UseInMediaSection)
						{
							//get the media item
							var media = sender.Services.MediaService.GetById(Convert.ToInt32(node.Id));

							alias = media.ContentType.Alias;
							nodeId = media.Id;
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

							if (config != null && !string.IsNullOrEmpty(config.ClickAction))
							{
								node.RoutePath = config.ClickAction;
							}
						}
					}
				}
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
		/// Sets the custom menus, based on your config
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void SetCustomMenus(Umbraco.Web.Trees.TreeControllerBase sender, Umbraco.Web.Trees.MenuRenderingEventArgs e)
		{
			//if they've set the ignore for Admins option, don't parse these rules
			if (!(Config.Instance.IgnoreForAdmin && umbraco.BusinessLogic.User.GetCurrent().IsAdmin()))
			{
				//only call if the node has a menu in the first place
				if (e.Menu != null && (sender.TreeAlias == "content" || sender.TreeAlias == "media"))
				{
					string alias = string.Empty;
					int nodeId = 0;
					string path = string.Empty;

					ConfigItem config = null;

					if (sender.TreeAlias == "content" && Convert.ToInt32(e.NodeId) > 0)
					{
						//get the document
						var content = sender.Services.ContentService.GetById(Convert.ToInt32(e.NodeId));

						alias = content.ContentType.Alias;
						nodeId = content.Id;
						path = content.Path;
					}
					else if (sender.TreeAlias == "media" && Convert.ToInt32(e.NodeId) > 0 && Config.Instance.UseInMediaSection)
					{
						//get the media item
						var media = sender.Services.MediaService.GetById(int.Parse(e.NodeId));

						alias = media.ContentType.Alias;
						nodeId = media.Id;
						path = media.Path;
					}
					else if (e.NodeId == "-1")
					{
						//it's the root node
						nodeId = -1;
						path = "-1";
					}

					//only carry on if we have a node id greater than 0
					if (nodeId != 0)
					{
						if (Config.Instance.ConfigEntries.Any(o => o.NodeId == nodeId && o.DocTypeAlias == sender.TreeAlias))
						{
							config = Config.Instance.ConfigEntries.First(o => o.NodeId == nodeId && o.DocTypeAlias == sender.TreeAlias);
						}
						if (Config.Instance.ConfigEntries.Any(o => o.DocTypeAlias == alias))
						{
							//doctype match, get the config item
							config = Config.Instance.ConfigEntries.First(o => o.DocTypeAlias == alias);
						}
						else if (Config.Instance.ConfigEntries.Any(o => o.NodeId == nodeId && string.IsNullOrEmpty(o.DocTypeAlias)))
						{
							//node id match, get the config item
							config = Config.Instance.ConfigEntries.First(o => o.NodeId == nodeId);
						}

						if (config != null)
						{
							//first clear the current menu items, but only if there are things we can do with it
							if (config.MenuItems.Count > 0)
							{
								e.Menu.Items.Clear();
							}

							//get the list of actions the user is allowed to do on this node.
							List<IAction> allowedActions = umbraco.BusinessLogic.Actions.Action.FromString(umbraco.BusinessLogic.User.GetCurrent().GetPermissions(path));

							//now loop through and add the items from the list, checking that the current user has permissions for the item in the first place
							foreach (var action in config.MenuItems)
							{
								if (!action.CanBePermissionAssigned || (action.CanBePermissionAssigned && allowedActions.Contains(action)))
								{
									e.Menu.Items.Add(new Umbraco.Web.Models.Trees.MenuItem(action));
								}
							}

							//loop through the remove menu items and remove them
							foreach (var action in config.RemoveMenuItems)
							{
								if (e.Menu.Items.Any(a => a.Alias == action.Alias))
								{
									e.Menu.Items.Remove(e.Menu.Items.First(a => a.Alias == action.Alias));
								}
							}

							if (config.RemoveMenuItems.Any(a => a.Alias == e.Menu.DefaultMenuAlias) || !string.IsNullOrEmpty(config.ClickAction))
							{
								e.Menu.DefaultMenuAlias = string.Empty;
							}
						}
					}
				}
			}
		}
	}
}