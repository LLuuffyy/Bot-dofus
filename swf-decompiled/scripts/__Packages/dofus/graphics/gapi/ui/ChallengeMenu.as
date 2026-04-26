class dofus.graphics.gapi.ui.ChallengeMenu extends dofus.graphics.gapi.core.DofusAdvancedComponent
{
   var _bCancelButton;
   var _bReady;
   var _btnCancel;
   var _btnReady;
   var _lblCancel;
   var _lblReady;
   var _mcBackground;
   var _mcTick;
   var _sLabelCancel;
   var _sLabelReady;
   var addToQueue;
   var api;
   static var CLASS_NAME = "ChallengeMenu";
   static var X_OFFSET = 90;
   function ChallengeMenu()
   {
      super();
   }
   function set labelReady(sLabelReady)
   {
      this._sLabelReady = sLabelReady;
   }
   function set labelCancel(sLabelCancel)
   {
      this._sLabelCancel = sLabelCancel;
   }
   function set cancelButton(bCancelButton)
   {
      this._bCancelButton = bCancelButton;
      this._btnCancel._visible = bCancelButton;
      this._lblCancel._visible = bCancelButton;
      if(!bCancelButton)
      {
         this._mcBackground._x += dofus.graphics.gapi.ui.ChallengeMenu.X_OFFSET;
         this._btnReady._x += dofus.graphics.gapi.ui.ChallengeMenu.X_OFFSET;
         this._lblReady._x += dofus.graphics.gapi.ui.ChallengeMenu.X_OFFSET;
         this._mcTick._x += dofus.graphics.gapi.ui.ChallengeMenu.X_OFFSET;
      }
   }
   function set ready(bReady)
   {
      this._bReady = bReady;
      this._mcTick._visible = bReady;
   }
   function init()
   {
      super.init(false,dofus.graphics.gapi.ui.ChallengeMenu.CLASS_NAME);
   }
   function createChildren()
   {
      this.addToQueue({object:this,method:this.setLabels});
   }
   function setLabels()
   {
      this._lblReady.text = this._sLabelReady;
      if(this._bCancelButton)
      {
         this._lblCancel.text = this._sLabelCancel;
      }
   }
   function sendReadyState()
   {
      this.api.network.Game.ready(!this._bReady);
      this.ready = !this._bReady;
   }
   function sendCancel()
   {
      this.api.network.Game.leave();
   }
}
