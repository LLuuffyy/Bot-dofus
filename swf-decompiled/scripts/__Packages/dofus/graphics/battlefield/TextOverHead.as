class dofus.graphics.battlefield.TextOverHead extends dofus.graphics.battlefield.AbstractTextOverHead
{
   var _mcTxtBackground;
   var _oSprite;
   var _txtText;
   var _txtTitle;
   var addPvpGfxEffect;
   var addToQueue;
   var createTextField;
   var drawBackground;
   var drawGfx;
   var drawOrnamentos;
   function TextOverHead(sText, sFile, nColor, nFrame, oSprite, title, sFile2, nOrnamento)
   {
      super();
      this.initialize(title != undefined);
      this._oSprite = oSprite;
      this.addToQueue({object:this,method:this.addEventListeners});
      this.drawClip(sText,sFile,nColor,nFrame,this._oSprite.pvpGain,title,sFile2,nOrnamento);
   }
   function initialize(displayTitle)
   {
      super.initialize();
      this.createTextField("_txtText",30,0,-3 + dofus.graphics.battlefield.AbstractTextOverHead.HEIGHT_SPACER,0,0);
      if(displayTitle)
      {
         this.createTextField("_txtTitle",31,0,-3 + dofus.graphics.battlefield.AbstractTextOverHead.HEIGHT_SPACER,0,0);
         this._txtTitle.embedFonts = true;
      }
      this._txtText.embedFonts = true;
   }
   function addEventListeners()
   {
      this._oSprite.addEventListener("lpChanged",this);
   }
   function drawClip(sText, sFile, nColor, nFrame, nPvpGain, title, sFile2, nOrnamento)
   {
      var _loc11_ = sFile != undefined && nFrame != undefined;
      if(nPvpGain == undefined)
      {
         nPvpGain = 0;
      }
      this.initTextField(this._txtText,sText,nColor,dofus.graphics.battlefield.AbstractTextOverHead.TEXT_FORMAT);
      var _loc12_;
      var _loc13_;
      if(title)
      {
         this.initTextField(this._txtTitle,title.text,title.color,dofus.graphics.battlefield.AbstractTextOverHead.TEXT_FORMAT2);
         this._txtTitle._y = this._txtText._y + dofus.graphics.battlefield.AbstractTextOverHead.HEIGHT_SPACER + this._txtText.textHeight;
         _loc12_ = Math.ceil(this._txtText.textHeight + this._txtTitle.textHeight + dofus.graphics.battlefield.AbstractTextOverHead.HEIGHT_SPACER * 3);
         _loc13_ = Math.ceil(Math.max(this._txtText.textWidth,this._txtTitle.textWidth) + dofus.graphics.battlefield.AbstractTextOverHead.WIDTH_SPACER * 2);
      }
      else
      {
         _loc12_ = Math.ceil(this._txtText.textHeight + dofus.graphics.battlefield.AbstractTextOverHead.HEIGHT_SPACER * 2);
         _loc13_ = Math.ceil(this._txtText.textWidth + dofus.graphics.battlefield.AbstractTextOverHead.WIDTH_SPACER * 2);
      }
      var _loc14_;
      if(!_global.isNaN(nOrnamento) && nOrnamento > 0)
      {
         _loc14_ = 0;
         if(_loc13_ < 120)
         {
            _loc13_ = 120;
         }
         if(_loc12_ < 30)
         {
            _loc14_ = (30 - _loc12_) / 2;
            _loc12_ = 30;
         }
         this.drawOrnamentos(sFile2,nOrnamento,{ancho:_loc13_,alto:_loc12_,medio:_loc14_});
      }
      else
      {
         this.drawBackground(_loc13_,_loc12_,dofus.graphics.battlefield.AbstractTextOverHead.BACKGROUND_COLOR);
      }
      if(_loc11_)
      {
         this.drawGfx(sFile,nFrame);
         this.addPvpGfxEffect(nPvpGain,nFrame);
      }
   }
   function initTextField(txtField, sText, nColor, textFormat)
   {
      txtField.autoSize = "center";
      txtField.text = sText;
      txtField.selectable = false;
      txtField.setTextFormat(textFormat);
      if(nColor != undefined)
      {
         txtField.textColor = nColor;
      }
   }
   function lpChanged(oEvent)
   {
      var _loc3_ = this._oSprite.name + " (" + this._oSprite.LP + ")";
      this.initTextField(this._txtText,_loc3_,undefined,dofus.graphics.battlefield.AbstractTextOverHead.TEXT_FORMAT);
      this._mcTxtBackground.clear();
      var _loc4_ = Math.ceil(this._txtText.textHeight + dofus.graphics.battlefield.AbstractTextOverHead.HEIGHT_SPACER * 2);
      var _loc5_ = Math.ceil(this._txtText.textWidth + dofus.graphics.battlefield.AbstractTextOverHead.WIDTH_SPACER * 2);
      this.drawBackground(_loc5_,_loc4_,dofus.graphics.battlefield.AbstractTextOverHead.BACKGROUND_COLOR);
   }
}
