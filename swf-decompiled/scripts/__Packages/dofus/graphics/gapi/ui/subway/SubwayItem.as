class dofus.graphics.gapi.ui.subway.SubwayItem extends dofus.graphics.gapi.core.DofusAdvancedComponent
{
   var _btnLocate;
   var _lblArea;
   var _lblCoords;
   var _lblCost;
   var _lblName;
   var _mcKamas;
   var _mcList;
   var _mcUnderAttack;
   var _mcUnderAttackInteractivity;
   var _oItem;
   var addToQueue;
   var api;
   function SubwayItem()
   {
      super();
      this._mcUnderAttack._visible = false;
      this._mcUnderAttackInteractivity._visible = false;
      this.api = _global.API;
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
         this._lblCost.text = oItem.cost;
         this._lblCoords.text = oItem.coordinates;
         this._lblName.text = oItem.name;
         this._lblArea.text = oItem.areaName;
         if(oItem.areaName == undefined)
         {
            this._lblName._y = 3;
         }
         this._btnLocate._visible = true;
         this._mcKamas._visible = oItem.cost > 0;
         if(this._oItem.attackNear)
         {
            this._mcUnderAttack._visible = true;
            this._mcUnderAttackInteractivity._visible = true;
            var ref = this;
            this._mcUnderAttackInteractivity.onRollOver = function()
            {
               ref.over({target:this});
            };
            this._mcUnderAttackInteractivity.onRollOut = function()
            {
               ref.out({target:this});
            };
         }
         else
         {
            this._mcUnderAttack._visible = false;
            this._mcUnderAttackInteractivity._visible = false;
         }
      }
      else if(this._lblCost.text != undefined)
      {
         this._lblCost.text = "";
         this._lblCoords.text = "";
         this._lblName.text = "";
         this._lblArea.text = "";
         this._btnLocate._visible = false;
         this._mcUnderAttack._visible = false;
         this._mcUnderAttackInteractivity._visible = false;
         this._mcKamas._visible = false;
      }
   }
   function init()
   {
      super.init(false);
   }
   function createChildren()
   {
      this.addToQueue({object:this,method:this.addListeners});
   }
   function addListeners()
   {
      this._btnLocate.addEventListener("click",this);
   }
   function click(oEvent)
   {
      this.api.kernel.GameManager.updateCompass(this._oItem.x,this._oItem.y,true);
   }
   function over(event)
   {
      var _loc0_;
      if((_loc0_ = event.target) === this._mcUnderAttackInteractivity)
      {
         this.api.ui.showTooltip(this.api.lang.getText("CONQUEST_NEAR_PRISM_UNDER_ATTACK"),_root._xmouse,_root._ymouse);
      }
   }
   function out(event)
   {
      this.api.ui.hideTooltip();
   }
}
