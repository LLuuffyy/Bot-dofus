class dofus.graphics.battlefield.CraftResultOverHead extends ank.gapi.core.UIBasicComponent
{
   var _ldrItem;
   var _mcBack;
   var _mcCross;
   var _mcMiss;
   var attachMovie;
   function CraftResultOverHead(bAdd, oItem)
   {
      super();
      this.initialize();
      this.drawClip(bAdd,oItem);
   }
   function get height()
   {
      return 33;
   }
   function get width()
   {
      return 62;
   }
   function initialize()
   {
      this.attachMovie("CraftResultOverHeadBubble","_mcBack",10);
   }
   function reverseClip()
   {
      this._mcBack._yscale = -100;
      this._mcBack._y += this._mcBack._height - 7;
   }
   function drawClip(bAdd, oItem)
   {
      if(oItem == undefined)
      {
         this.attachMovie("CraftResultOverHeadCross","_mcCross",40);
         this._ldrItem.removeMovieClip();
      }
      else
      {
         this.attachMovie("GAPILoader","_ldrItem",20,{_x:6,_y:4,_width:20,_height:20,scaleContent:true,contentPath:oItem.iconFile});
         if(_global.API.datacenter.Basics.aks_current_server.isTemporis())
         {
            this._ldrItem.holder.filters = undefined;
            if(oItem.realUnicId >= dofus.Constants.REFFINED_ITEM.minimumID)
            {
               oItem.addGlowOnItemIcon(this._ldrItem.holder,dofus.Constants.REFFINED_ITEM.color,dofus.Constants.REFFINED_ITEM.alpha,dofus.Constants.REFFINED_ITEM.blur,dofus.Constants.REFFINED_ITEM.intensity);
            }
            else if(oItem.realUnicId >= dofus.Constants.IMPROVED_ITEM.minimumID)
            {
               oItem.addGlowOnItemIcon(this._ldrItem.holder,dofus.Constants.IMPROVED_ITEM.color,dofus.Constants.IMPROVED_ITEM.alpha,dofus.Constants.IMPROVED_ITEM.blur,dofus.Constants.IMPROVED_ITEM.intensity);
            }
         }
         this._mcCross.removeMovieClip();
      }
      if(!bAdd)
      {
         this.attachMovie("CraftResultOverHeadMiss","_mcMiss",30);
      }
      else
      {
         this._mcMiss.removeMovieClip();
      }
   }
}
