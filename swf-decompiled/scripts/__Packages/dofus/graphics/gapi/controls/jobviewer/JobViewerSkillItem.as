class dofus.graphics.gapi.controls.jobviewer.JobViewerSkillItem extends ank.gapi.core.UIBasicComponent
{
   var __height;
   var _ctrIcon;
   var _lblQuantity;
   var _lblSkill;
   var _lblSource;
   var _mcArrow;
   var _mcList;
   var _parent;
   var addToQueue;
   function JobViewerSkillItem()
   {
      super();
   }
   function set list(mcList)
   {
      this._mcList = mcList;
   }
   function setValue(bUsed, sSuggested, oItem)
   {
      var _loc5_;
      var _loc6_;
      var _loc7_;
      var _loc8_;
      if(bUsed)
      {
         this._mcArrow._visible = true;
         this._lblSkill.text = oItem.description;
         this._lblSource.text = oItem.interactiveObject != undefined ? oItem.interactiveObject : "";
         this._lblSkill.setSize(this._lblSource.width - this._lblSource.textWidth - 15,this.__height);
         if(oItem.item != undefined)
         {
            if(oItem.param1 == oItem.param2)
            {
               _loc5_ = "(#4s)  #1";
            }
            else
            {
               _loc5_ = "(#4s)  #1{~2 " + this._mcList.gapi.api.lang.getText("TO_RANGE") + " }#2";
            }
            this._lblQuantity.text = ank.utils.PatternDecoder.getDescription(_loc5_,[oItem.param1,oItem.param2,oItem.param3,Math.round(oItem.param4 / 100) / 10]);
            this._ctrIcon.contentData = oItem.item;
         }
         else
         {
            _loc6_ = this._parent._parent._parent._parent;
            _loc7_ = ank.utils.PatternDecoder.combine(this._mcList.gapi.api.lang.getText("SLOT"),"n",oItem.param1 < 2);
            _loc8_ = "#1 " + _loc7_ + " (#2%)";
            this._lblQuantity.text = ank.utils.PatternDecoder.getDescription(_loc8_,[oItem.param1,oItem.param4]);
            this._ctrIcon.contentData = undefined;
         }
      }
      else if(this._lblSource.text != undefined)
      {
         this._mcArrow._visible = false;
         this._lblSource.text = "";
         this._lblSkill.text = "";
         this._lblQuantity.text = "";
         this._ctrIcon.contentData = undefined;
      }
   }
   function init()
   {
      super.init(false);
      this._mcArrow._visible = false;
      this.addToQueue({object:this,method:this.addListeners});
   }
   function addListeners()
   {
      this._ctrIcon.addEventListener("over",this);
      this._ctrIcon.addEventListener("out",this);
   }
   function over(oEvent)
   {
      var _loc3_ = oEvent.target.contentData;
      this._mcList._parent._parent.gapi.showTooltip(_loc3_.name,oEvent.target,-20);
   }
   function out(oEvent)
   {
      this._mcList._parent._parent.gapi.hideTooltip();
   }
}
