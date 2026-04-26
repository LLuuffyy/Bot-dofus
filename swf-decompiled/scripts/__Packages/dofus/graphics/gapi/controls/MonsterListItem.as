class dofus.graphics.gapi.controls.MonsterListItem extends dofus.graphics.gapi.core.DofusAdvancedComponent
{
   var _ldrSprite;
   var _sContentPath;
   var addToQueue;
   var initialized;
   static var CLASS_NAME = "MonsterListItem";
   static var SIZE = 20;
   var sAnim = "StaticR";
   function MonsterListItem()
   {
      super();
   }
   function get contentPath()
   {
      return this._sContentPath;
   }
   function set contentPath(sContentPath)
   {
      this._sContentPath = sContentPath;
      if(this.initialized)
      {
         this.beginDisplay();
      }
   }
   function init()
   {
      super.init(false,dofus.graphics.gapi.controls.MonsterListItem.CLASS_NAME);
   }
   function createChildren()
   {
      this.addToQueue({object:this,method:this.addListeners});
      this.addToQueue({object:this,method:this.beginDisplay});
   }
   function addListeners()
   {
      this._ldrSprite.addEventListener("initialization",this);
   }
   function initialization(oEvent)
   {
      var mc = oEvent.clip;
      mc._visible = false;
      mc.attachMovie("staticR","mcAnim",10);
      var monsterListItem = this;
      var nCompteur = 0;
      mc.onEnterFrame = function()
      {
         nCompteur++;
         if(nCompteur >= 3)
         {
            monsterListItem.prepareSprite(this);
            delete mc.onEnterFrame;
         }
      };
   }
   function prepareSprite(mc)
   {
      if(mc._width > mc._height)
      {
         mc._xscale = mc._yscale = 100 * dofus.graphics.gapi.controls.MonsterListItem.SIZE / mc._width;
      }
      else
      {
         mc._yscale = mc._xscale = 100 * dofus.graphics.gapi.controls.MonsterListItem.SIZE / mc._height;
      }
      mc._visible = true;
   }
   function beginDisplay()
   {
      if(this.contentPath == undefined)
      {
         return undefined;
      }
      this._ldrSprite.forceReload = true;
      this._ldrSprite.content.removeMovieClip();
      this._ldrSprite.contentPath = this.contentPath;
   }
}
