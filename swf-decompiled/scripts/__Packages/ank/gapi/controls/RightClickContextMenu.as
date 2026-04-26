class ank.gapi.controls.RightClickContextMenu extends ContextMenu
{
   var onSelect;
   function RightClickContextMenu(oAPI, callbackFunction)
   {
      super(callbackFunction);
      this.hideBuiltInItems();
      var proto = ank.gapi.controls.RightClickContextMenu.prototype;
      this.onSelect = function()
      {
         proto.onRightClick(oAPI);
      };
   }
   function onRightClick(api)
   {
      api.ui.hideTooltip();
      var _loc4_ = ank.gapi.controls.PopupMenu.currentPopupMenu;
      if(_loc4_ != undefined)
      {
         _loc4_.onMouseUp();
      }
      api.mouseClicksMemorizer.storeCurrentMouseClick(true);
      if(api.gfx.rollOverMcSprite != undefined && !(api.gfx.rollOverMcSprite.data instanceof dofus.datacenter.Character))
      {
         api.gfx.onSpriteRelease(api.gfx.rollOverMcSprite,true);
         return undefined;
      }
      if(api.gfx.rollOverMcObject != undefined)
      {
         api.gfx.onObjectRelease(api.gfx.rollOverMcObject,true);
         return undefined;
      }
      var _loc5_ = api.ui.uiComponents;
      var _loc6_ = 0;
      var _loc7_;
      var _loc8_;
      var _loc9_;
      while(_loc6_ < _loc5_.length)
      {
         _loc7_ = api.ui.getUIComponent(_loc5_[_loc6_].name);
         if(_loc7_ != undefined)
         {
            if(_loc7_.getCurrentTab().currentOverItem instanceof dofus.datacenter.Item)
            {
               if(api.ui.getUIComponent("ItemViewer") != undefined)
               {
                  api.ui.unloadUIComponent("ItemViewer");
               }
               _loc8_ = api.ui.loadUIComponent("ItemViewer","ItemViewer",{item:_loc7_.getCurrentTab().currentOverItem},{bAlwaysOnTop:true});
               _loc8_.hideWarning();
               return undefined;
            }
            _loc9_ = _loc7_.currentOverItem;
            if(_loc9_ instanceof dofus.datacenter.Item)
            {
               _loc7_.itemViewer.createActionPopupMenu(_loc9_);
               return undefined;
            }
            if(_loc9_ instanceof dofus.datacenter.Spell)
            {
               if(api.datacenter.Game.isFight)
               {
                  api.ui.getUIComponent("Banner").shortcuts.castSpellOnSelf(_loc9_);
               }
               else
               {
                  _loc7_.createSpellActionPopupMenu(_loc9_);
               }
               return undefined;
            }
            if(_loc9_ instanceof dofus.datacenter.InventoryShortcutItem)
            {
               _loc7_.createInventoryShortcutItemActionPopupMenu(_loc9_);
               return undefined;
            }
         }
         _loc6_ += 1;
      }
      var _loc10_;
      if(api.datacenter.Basics.inGame && api.datacenter.Player.isAuthorized)
      {
         _loc10_ = api.kernel.AdminManager.getAdminPopupMenu(api.datacenter.Player.Name,true);
         _loc10_.addItem("Client v" + dofus.Constants.VERSION + "." + dofus.Constants.SUBVERSION + "." + dofus.Constants.SUBSUBVERSION + " >>",this,this.printRightClickPopupMenu,[api]);
         _loc10_.items.unshift(_loc10_.items.pop());
         _loc10_.show(_root._xmouse,_root._ymouse,true);
      }
      else
      {
         this.printRightClickPopupMenu(api);
      }
   }
   function printRightClickPopupMenu(api)
   {
      var _loc2_ = api.ui.createPopupMenu();
      _loc2_.addStaticItem("DOFUS RETRO Client v" + dofus.Constants.VERSION + "." + dofus.Constants.SUBVERSION + "." + dofus.Constants.SUBSUBVERSION);
      _loc2_.addStaticItem("Flash player " + System.capabilities.version);
      var o = {};
      var gapi = api.ui;
      o.selectQualities = function()
      {
         var _loc1_ = gapi.createPopupMenu();
         _loc1_.addStaticItem(api.lang.getText("OPTION_DEFAULTQUALITY"));
         _loc1_.addItem(api.lang.getText("QUALITY_LOW"),o,o.setQualityOption,["low"],o.getOption("DefaultQuality") != "low");
         _loc1_.addItem(api.lang.getText("QUALITY_MEDIUM"),o,o.setQualityOption,["medium"],o.getOption("DefaultQuality") != "medium");
         _loc1_.addItem(api.lang.getText("QUALITY_HIGH"),o,o.setQualityOption,["high"],o.getOption("DefaultQuality") != "high");
         _loc1_.show();
      };
      o.setQualityOption = function(sQuality)
      {
         o.setOption("DefaultQuality",sQuality);
      };
      o.setOption = function(sKey, mValue)
      {
         api.kernel.OptionsManager.setOption(sKey,mValue);
      };
      o.getOption = function(sKey)
      {
         return api.kernel.OptionsManager.getOption(sKey);
      };
      _loc2_.addItem(api.lang.getText("OPTION_DEFAULTQUALITY") + " >>",o,o.selectQualities);
      _loc2_.addItem(api.lang.getText("OPTIONS"),gapi,gapi.loadUIComponent,["Options","Options",{_y:(gapi.screenHeight != 432 ? 0 : -50)},{bAlwaysOnTop:true}]);
      _loc2_.addItem(api.lang.getText("OPTION_MOVABLEBAR"),o,function(sKey, mValue)
      {
         o.setOption(sKey,mValue);
         api.kernel.OptionsManager.onMovableBarOptionChanged();
      }
      ,["MovableBar",!o.getOption("MovableBar")]);
      _loc2_.show(_root._xmouse,_root._ymouse,true);
   }
}
