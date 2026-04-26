class dofus.graphics.battlefield.GuildOverHead extends dofus.graphics.battlefield.AbstractTextOverHead
{
   var _txtGuildName;
   var _txtSpriteName;
   var _txtTitle;
   var attachMovie;
   var createTextField;
   function GuildOverHead(sText, sSpriteName, oEmblem, sFile, nFrame, nPvpGain, title, sFile2, nOrnamento)
   {
      super();
      this.initialize(title != undefined);
      this.drawClip(sText,sSpriteName,oEmblem,sFile,nFrame,nPvpGain,title,sFile2,nOrnamento);
   }
   function initialize(displayTitle)
   {
      super.initialize();
      this.createTextField("_txtGuildName",30,0,-2 + dofus.graphics.battlefield.AbstractTextOverHead.HEIGHT_SPACER,0,0);
      this.createTextField("_txtSpriteName",40,0,13 + dofus.graphics.battlefield.AbstractTextOverHead.HEIGHT_SPACER,0,0);
      if(displayTitle)
      {
         this.createTextField("_txtTitle",31,0,-2 + dofus.graphics.battlefield.AbstractTextOverHead.HEIGHT_SPACER,0,0);
         this._txtTitle.embedFonts = true;
      }
   }
   function drawClip(sGuildName, sSpriteName, oEmblem, sFile, nFrame, nPvpGain, title, sFile2, nOrnamento)
   {
      var _loc11_ = sFile != undefined && nFrame != undefined;
      if(nPvpGain == undefined)
      {
         nPvpGain = 0;
      }
      this._txtGuildName.embedFonts = true;
      this._txtGuildName.autoSize = "left";
      this._txtGuildName.text = sGuildName;
      this._txtGuildName.selectable = false;
      this._txtGuildName.setTextFormat(dofus.graphics.battlefield.AbstractTextOverHead.TEXT_SMALL_FORMAT);
      this._txtSpriteName.embedFonts = true;
      this._txtSpriteName.autoSize = "left";
      this._txtSpriteName.text = sSpriteName;
      this._txtSpriteName.selectable = false;
      this._txtSpriteName.setTextFormat(dofus.graphics.battlefield.AbstractTextOverHead.TEXT_FORMAT);
      var _loc12_ = 0;
      var _loc13_;
      var _loc14_;
      var _loc15_;
      if(title)
      {
         this._txtTitle.autoSize = "center";
         this._txtTitle.text = title.text;
         this._txtTitle.selectable = false;
         this._txtTitle.setTextFormat(dofus.graphics.battlefield.AbstractTextOverHead.TEXT_FORMAT2);
         if(title.color != undefined)
         {
            this._txtTitle.textColor = title.color;
         }
         _loc13_ = Math.ceil(30 + dofus.graphics.battlefield.AbstractTextOverHead.HEIGHT_SPACER * 3 + this._txtTitle.textHeight);
         _loc14_ = Math.ceil(Math.max(this._txtGuildName.textWidth,this._txtSpriteName.textWidth) + dofus.graphics.battlefield.AbstractTextOverHead.WIDTH_SPACER * 4) + 30;
         _loc14_ = Math.ceil(Math.max(_loc14_,this._txtTitle.textWidth + dofus.graphics.battlefield.AbstractTextOverHead.WIDTH_SPACER * 2));
         _loc12_ = dofus.graphics.battlefield.AbstractTextOverHead.HEIGHT_SPACER + this._txtTitle.textHeight;
         this._txtGuildName._x = this._txtSpriteName._x = (- _loc14_) / 2 + 30 + dofus.graphics.battlefield.AbstractTextOverHead.WIDTH_SPACER * 2;
         this._txtTitle._y = 27 + dofus.graphics.battlefield.AbstractTextOverHead.HEIGHT_SPACER * 2;
      }
      else
      {
         _loc13_ = Math.ceil(30 + dofus.graphics.battlefield.AbstractTextOverHead.HEIGHT_SPACER * 2);
         _loc14_ = Math.ceil(Math.max(this._txtGuildName.textWidth,this._txtSpriteName.textWidth) + dofus.graphics.battlefield.AbstractTextOverHead.WIDTH_SPACER * 4) + 30;
         this._txtGuildName._x = this._txtSpriteName._x = (- _loc14_) / 2 + 30 + dofus.graphics.battlefield.AbstractTextOverHead.WIDTH_SPACER * 2;
      }
      var _loc16_;
      if(!isNaN(nOrnamento) && nOrnamento > 0)
      {
         _loc16_ = 0;
         if(_loc14_ < 120)
         {
            _loc14_ = 120;
         }
         if(_loc13_ < 30)
         {
            _loc16_ = (30 - _loc13_) / 2;
            _loc13_ = 30;
         }
         this.drawOrnamentos(sFile2,nOrnamento,{ancho:_loc14_,alto:_loc13_,medio:_loc16_});
      }
      else
      {
         this.drawBackground(_loc14_,_loc13_,dofus.graphics.battlefield.AbstractTextOverHead.BACKGROUND_COLOR);
      }
      this.attachMovie("Emblem","_eEmblem",100,{_x:Math.ceil((- _loc14_) / 2) + dofus.graphics.battlefield.AbstractTextOverHead.WIDTH_SPACER,_y:dofus.graphics.battlefield.AbstractTextOverHead.HEIGHT_SPACER,_width:30,_height:30,data:oEmblem,shadow:true});
      if(_loc11_)
      {
         this.drawGfx(sFile,nFrame);
         this.addPvpGfxEffect(nPvpGain,nFrame);
      }
   }
}
