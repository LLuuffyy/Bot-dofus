class dofus.graphics.gapi.controls.listinventoryviewer.ListInventoryViewerItemNoPrice extends ank.gapi.core.UIBasicComponent
{
   var __height;
   var __width;
   var _lblName;
   var _ldrIcon;
   var _mcMask;
   var addToQueue;
   var createEmptyMovieClip;
   var drawRoundRect;
   var getNextHighestDepth;
   function ListInventoryViewerItemNoPrice()
   {
      super();
   }
   function setValue(bUsed, sSuggested, oItem)
   {
      var _loc5_;
      if(bUsed)
      {
         this._lblName.text = !bUsed ? "" : (oItem.Quantity <= 1 ? "" : "x" + oItem.label + " ") + oItem.name;
         if(oItem.isMonsterInBidHouse)
         {
            if(this._mcMask == undefined)
            {
               _loc5_ = this.createEmptyMovieClip("_mcMask",this.getNextHighestDepth());
               this.drawRoundRect(_loc5_,0,0,20,20,0,0);
               this._ldrIcon.setMask(_loc5_);
            }
            this._ldrIcon._x = 10;
            this._ldrIcon._y = 16.5;
         }
         else
         {
            this._ldrIcon._x = 2;
            this._ldrIcon._y = 2;
         }
         this._ldrIcon.forceReload = !oItem.isMonsterInBidHouse ? false : true;
         this._ldrIcon.contentParams = oItem.params;
         this._ldrIcon.contentPath = !bUsed ? "" : oItem.iconFile;
         if(_global.API.datacenter.Basics.aks_current_server.isTemporis())
         {
            this._ldrIcon.holder.filters = undefined;
            if(oItem.realUnicId >= dofus.Constants.REFFINED_ITEM.minimumID)
            {
               oItem.addGlowOnItemIcon(this._ldrIcon.holder,dofus.Constants.REFFINED_ITEM.color,dofus.Constants.REFFINED_ITEM.alpha,dofus.Constants.REFFINED_ITEM.blur,dofus.Constants.REFFINED_ITEM.intensity);
            }
            else if(oItem.realUnicId >= dofus.Constants.IMPROVED_ITEM.minimumID)
            {
               oItem.addGlowOnItemIcon(this._ldrIcon.holder,dofus.Constants.IMPROVED_ITEM.color,dofus.Constants.IMPROVED_ITEM.alpha,dofus.Constants.IMPROVED_ITEM.blur,dofus.Constants.IMPROVED_ITEM.intensity);
            }
         }
         this._lblName.styleName = oItem.style != "" ? oItem.style + "LeftSmallLabel" : "BrownLeftSmallLabel";
      }
      else if(this._lblName.text != undefined)
      {
         this._ldrIcon._x = 2;
         this._ldrIcon._y = 2;
         this._lblName.text = "";
         this._ldrIcon.contentPath = "";
      }
   }
   function init()
   {
      super.init(false);
   }
   function createChildren()
   {
      this.arrange();
   }
   function size()
   {
      super.size();
      this.addToQueue({object:this,method:this.arrange});
   }
   function arrange()
   {
      this._lblName.setSize(this.__width - 20,this.__height);
   }
}
