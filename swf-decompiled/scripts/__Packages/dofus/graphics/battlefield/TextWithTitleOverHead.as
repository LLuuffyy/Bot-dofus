class dofus.graphics.battlefield.TextWithTitleOverHead extends dofus.graphics.battlefield.AbstractTextOverHead
{
   var _aStars;
   var _nFullWidth;
   var _nStarsValue;
   var _sExperiencia;
   var _txtExperiencia;
   var _txtText;
   var _txtTitle;
   var createEmptyMovieClip;
   var createTextField;
   var drawBackground;
   var drawGfx;
   static var HEIGHT_LINE;
   static var STARS_COUNT = 5;
   static var STARS_WIDTH = 10;
   static var STARS_MARGIN = 2;
   static var STARS_COLORS = [-1,16777011,16750848,39168,39372,6697728,2236962,16711680,65280,16777215,16711935];
   function TextWithTitleOverHead(sText, sFile, nColor, nFrame, sTitle, nColorTitle, nStarsValue, nExperiencia)
   {
      super();
      this.initialize(nStarsValue,nExperiencia);
      this.drawClip(sText,sFile,nColor,nFrame,sTitle,nColorTitle);
   }
   function initialize(starValue, nExperiencia)
   {
      super.initialize();
      if(starValue == undefined || _global.isNaN(starValue))
      {
         starValue = -1;
      }
      if(_global.isNaN(nExperiencia))
      {
         nExperiencia;
      }
      this._sExperiencia = nExperiencia;
      this._nStarsValue = starValue;
      this.createTextField("_txtTitle",40,0,-3 + dofus.graphics.battlefield.AbstractTextOverHead.HEIGHT_SPACER + 4,0,0);
      this.createTextField("_txtText",30,0,-3 + dofus.graphics.battlefield.AbstractTextOverHead.HEIGHT_SPACER * (this._nStarsValue <= -1 ? 1 : 2) + 26 + (this._nStarsValue <= -1 ? 0 : dofus.graphics.battlefield.TextWithTitleOverHead.STARS_WIDTH),0,0);
      var _loc6_;
      if(this._sExperiencia != undefined)
      {
         _loc6_ = this._txtTitle._y + this._txtTitle.textHeight + dofus.graphics.battlefield.AbstractTextOverHead.HEIGHT_SPACER + 8;
         this.createTextField("_txtExperiencia",1010,0,_loc6_,0,0);
      }
      _loc6_ = dofus.graphics.battlefield.AbstractTextOverHead.HEIGHT_SPACER;
      _loc6_ += dofus.graphics.battlefield.TextWithTitleOverHead.HEIGHT_LINE + dofus.graphics.battlefield.AbstractTextOverHead.HEIGHT_SPACER;
      if(this._sExperiencia)
      {
         _loc6_ += dofus.graphics.battlefield.TextWithTitleOverHead.HEIGHT_LINE + dofus.graphics.battlefield.AbstractTextOverHead.HEIGHT_SPACER;
      }
      if(this._nStarsValue > 0)
      {
         _loc6_ += dofus.graphics.battlefield.TextWithTitleOverHead.HEIGHT_LINE + dofus.graphics.battlefield.AbstractTextOverHead.HEIGHT_SPACER;
      }
      this._txtText.embedFonts = true;
      this._txtTitle.embedFonts = true;
      this._txtExperiencia.embedFonts = true;
      this._aStars = [];
   }
   function drawClip(sText, sFile, nColor, nFrame, sTitle, nColorTitle)
   {
      var _loc8_ = sFile != undefined && nFrame != undefined;
      this._txtText.autoSize = "center";
      this._txtText.text = sText;
      this._txtText.selectable = false;
      this._txtText.setTextFormat(dofus.graphics.battlefield.AbstractTextOverHead.TEXT_FORMAT);
      if(nColor != undefined)
      {
         this._txtText.textColor = nColor;
      }
      this._txtTitle.autoSize = "center";
      this._txtTitle.text = sTitle;
      this._txtTitle.selectable = false;
      this._txtTitle.setTextFormat(dofus.graphics.battlefield.AbstractTextOverHead.TEXT_FORMAT);
      if(nColorTitle != undefined)
      {
         this._txtTitle.textColor = nColorTitle;
      }
      var _loc9_;
      var _loc10_;
      var _loc11_;
      if(this._sExperiencia != undefined)
      {
         this._txtExperiencia.autoSize = "center";
         _loc9_ = "";
         _loc10_ = 0;
         _loc11_ = this._sExperiencia.length - 1;
         while(_loc11_ >= 0)
         {
            if(_loc10_ > 0 && _loc10_ % 3 == 0)
            {
               _loc9_ = "," + _loc9_;
            }
            _loc9_ = this._sExperiencia.substring(_loc11_,_loc11_ + 1) + _loc9_;
            _loc10_ += 1;
            _loc11_ -= 1;
         }
         this._txtExperiencia.text = _loc9_ + " EXP";
         this._txtExperiencia.selectable = false;
         this._txtExperiencia.setTextFormat(dofus.graphics.battlefield.AbstractTextOverHead.TEXT_FORMAT);
         this._txtExperiencia.textColor = 4489977;
      }
      this._nFullWidth = dofus.graphics.battlefield.TextWithTitleOverHead.STARS_COUNT * dofus.graphics.battlefield.TextWithTitleOverHead.STARS_WIDTH + (dofus.graphics.battlefield.TextWithTitleOverHead.STARS_COUNT - 1) * dofus.graphics.battlefield.TextWithTitleOverHead.STARS_MARGIN;
      var _loc12_ = Math.ceil(this._txtText.textHeight + 20 + dofus.graphics.battlefield.AbstractTextOverHead.HEIGHT_SPACER * (this._nStarsValue <= -1 ? 3 : 4) + (this._nStarsValue <= -1 ? 0 : dofus.graphics.battlefield.TextWithTitleOverHead.STARS_WIDTH));
      var _loc13_ = Math.ceil(Math.max(Math.max(this._txtText.textWidth,this._txtTitle.textWidth),this._nStarsValue <= -1 ? 0 : this._nFullWidth) + dofus.graphics.battlefield.AbstractTextOverHead.WIDTH_SPACER * 2);
      this.drawBackground(_loc13_,_loc12_,dofus.graphics.battlefield.AbstractTextOverHead.BACKGROUND_COLOR);
      var _loc14_;
      var _loc15_;
      var _loc16_;
      var _loc17_;
      var _loc18_;
      var _loc19_;
      if(this._nStarsValue > -1)
      {
         _loc14_ = this.getStarsColor();
         _loc15_ = 0;
         while(_loc15_ < dofus.graphics.battlefield.TextWithTitleOverHead.STARS_COUNT)
         {
            _loc16_ = {};
            _loc16_._x = _loc15_ * (dofus.graphics.battlefield.TextWithTitleOverHead.STARS_WIDTH + dofus.graphics.battlefield.TextWithTitleOverHead.STARS_MARGIN) - this._nFullWidth / 2 + dofus.graphics.battlefield.AbstractTextOverHead.WIDTH_SPACER;
            _loc19_ = this._txtTitle._y + this._txtTitle.textHeight + dofus.graphics.battlefield.AbstractTextOverHead.HEIGHT_SPACER;
            if(this._txtExperiencia != undefined)
            {
               _loc19_ += this._txtExperiencia.textHeight + dofus.graphics.battlefield.AbstractTextOverHead.HEIGHT_SPACER;
            }
            _loc16_._y = _loc19_;
            this._aStars[_loc15_] = this.createEmptyMovieClip("star" + _loc15_,50 + _loc15_);
            this._aStars[_loc15_].attachMovie("StarBorder","star",1,_loc16_);
            _loc17_ = this._aStars[_loc15_].star.fill;
            if(_loc14_[_loc15_] > -1)
            {
               _loc18_ = new Color(_loc17_);
               _loc18_.setRGB(_loc14_[_loc15_]);
            }
            else
            {
               _loc17_._alpha = 0;
            }
            _loc15_ += 1;
         }
      }
      if(_loc8_)
      {
         this.drawGfx(sFile,nFrame);
      }
   }
   function getStarsColor()
   {
      var _loc2_ = [];
      var _loc3_ = 0;
      var _loc4_;
      while(_loc3_ < dofus.graphics.battlefield.TextWithTitleOverHead.STARS_COUNT)
      {
         _loc4_ = Math.floor(this._nStarsValue / 100) + (this._nStarsValue - Math.floor(this._nStarsValue / 100) * 100 <= _loc3_ * (100 / dofus.graphics.battlefield.TextWithTitleOverHead.STARS_COUNT) ? 0 : 1);
         _loc2_[_loc3_] = dofus.graphics.battlefield.TextWithTitleOverHead.STARS_COLORS[Math.min(_loc4_,dofus.graphics.battlefield.TextWithTitleOverHead.STARS_COLORS.length - 1)];
         _loc3_ += 1;
      }
      return _loc2_;
   }
}
