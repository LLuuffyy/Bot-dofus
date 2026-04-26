class dofus.graphics.gapi.ui.friends.FriendsConnectedItem extends ank.gapi.core.UIBasicComponent
{
   var _btnRemove;
   var _lblLevel;
   var _lblName;
   var _ldrAlignement;
   var _ldrGuild;
   var _mcFight;
   var _mcList;
   var _oItem;
   var _parent;
   var addToQueue;
   var api;
   function FriendsConnectedItem()
   {
      super();
   }
   function set list(mcList)
   {
      this._mcList = mcList;
   }
   function setValue(bUsed, sSuggested, oItem)
   {
      if(bUsed)
      {
         this._oItem = oItem;
         this._lblName.text = oItem.name;
         if(oItem.level != undefined)
         {
            this._lblLevel.text = oItem.level;
         }
         else
         {
            this._lblLevel.text = "";
         }
         this._mcFight._visible = oItem.state == "IN_MULTI";
         this._ldrGuild.contentPath = dofus.Constants.GUILDS_MINI_PATH + oItem.gfxID + ".swf";
         if(oItem.alignement != -1)
         {
            this._ldrAlignement.contentPath = dofus.Constants.ALIGNMENTS_MINI_PATH + oItem.alignement + ".swf";
         }
         else
         {
            this._ldrAlignement.contentPath = "";
         }
         this._btnRemove._visible = true;
      }
      else if(this._lblName.text != undefined)
      {
         this._lblName.text = "";
         this._lblLevel.text = "";
         this._ldrAlignement.contentPath = "";
         this._mcFight._visible = false;
         this._ldrGuild.contentPath = "";
         this._btnRemove._visible = false;
      }
   }
   function init()
   {
      super.init(false);
      this._mcFight._visible = false;
      this._btnRemove._visible = false;
      this.api = this._mcList.gapi.api;
   }
   function createChildren()
   {
      this.addToQueue({object:this,method:this.addListeners});
   }
   function addListeners()
   {
      this._btnRemove.addEventListener("click",this);
      this._mcFight.onRollOver = function()
      {
         this._parent.over({target:this});
      };
      this._mcFight.onRollOut = function()
      {
         this._parent.out({target:this});
      };
      this._mcFight.onRelease = function()
      {
         this._parent.click({target:this});
      };
   }
   function click(oEvent)
   {
      var _loc0_;
      if((_loc0_ = oEvent.target._name) !== "_mcFight")
      {
         if(this._oItem.account != undefined)
         {
            this._mcList._parent._parent.removeFriend("*" + this._oItem.account);
         }
         else
         {
            this._mcList._parent._parent.removeFriend(this._oItem.name);
         }
      }
      else
      {
         this.api.network.GameActions.joinChallengeAsSpectator(0,this._oItem.id);
      }
   }
   function over(oEvent)
   {
      var _loc0_;
      if((_loc0_ = oEvent.target._name) !== "_mcFight")
      {
         this.api.ui.showTooltip(this.api.lang.getText("PSEUDO_DOFUS",[this._oItem.account]),oEvent.row.cellRenderer_mc,-20);
      }
      else
      {
         this.api.ui.showTooltip(this.api.lang.getText("CLICK_TO_JOIN_AS_SPECTATOR"),this._mcFight,-20);
      }
   }
   function out(oEvent)
   {
      this._mcList.gapi.api.ui.hideTooltip();
   }
}
