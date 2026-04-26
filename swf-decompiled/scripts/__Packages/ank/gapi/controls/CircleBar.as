class ank.gapi.controls.CircleBar extends ank.gapi.core.UIBasicComponent
{
   var __height;
   var __width;
   var _mcCircle;
   var _sBackgroundLink;
   var addToQueue;
   var attachMovie;
   var getStyle;
   var setMovieClipColor;
   static var CLASS_NAME = "CircleBar";
   var _nValue = 0;
   var _nMinimum = 0;
   var _nMaximum = 100;
   function CircleBar()
   {
      super();
   }
   function set background(sBackground)
   {
      this._sBackgroundLink = sBackground;
   }
   function set minimum(nMinimum)
   {
      this._nMinimum = Number(nMinimum);
   }
   function get minimum()
   {
      return this._nMinimum;
   }
   function set maximum(nMaximum)
   {
      this._nMaximum = Number(nMaximum);
   }
   function get maximum()
   {
      return this._nMaximum;
   }
   function set value(nValue)
   {
      if(nValue > this._nMaximum)
      {
         nValue = this._nMaximum;
      }
      if(nValue < this._nMinimum)
      {
         nValue = this._nMinimum;
      }
      this._nValue = Number(nValue);
      this.addToQueue({object:this,method:this.applyValue});
   }
   function get value()
   {
      return this._nValue;
   }
   function get trueValue()
   {
      return this._nValue / this._nMaximum * 100;
   }
   function init()
   {
      super.init(false,ank.gapi.controls.CircleBar.CLASS_NAME);
   }
   function createChildren()
   {
      this.attachMovie(this._sBackgroundLink,"_mcCircle",10);
   }
   function size()
   {
      this._mcCircle.setSize(this.__width,this.__height);
   }
   function draw()
   {
      this.setMovieClipColor(this._mcCircle,this.getStyle().bgcolor);
   }
   function applyValue()
   {
      var _loc2_ = this._nValue - this._nMinimum;
      if(_loc2_ == 0)
      {
         return undefined;
      }
      var _loc3_ = this._nMaximum - this._nMinimum;
      this._mcCircle.gotoAndStop(Math.floor(_loc2_ / _loc3_ * (100 + 1)));
   }
}
