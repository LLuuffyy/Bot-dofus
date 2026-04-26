class ank.utils.SWFLoader extends MovieClip
{
   var _aArgs;
   var _frameStart;
   var broadcastMessage;
   var swf_mc;
   function SWFLoader()
   {
      super();
      AsBroadcaster.initialize(this);
      this.initialize(0);
   }
   function initialize(frame, args)
   {
      this.clear();
      this._frameStart = frame;
      this._aArgs = args;
   }
   function clear()
   {
      this.createEmptyMovieClip("swf_mc",10);
   }
   function remove()
   {
      this.swf_mc.__proto__ = MovieClip.prototype;
      this.swf_mc.removeMovieClip();
   }
   function loadSWF(file, frame, args)
   {
      this.initialize(frame,args);
      var _loc5_ = new MovieClipLoader();
      _loc5_.addListener(this);
      _loc5_.loadClip(file,this.swf_mc);
   }
   function onLoadComplete(mc)
   {
      this.broadcastMessage("onLoadComplete",mc,this._aArgs);
   }
   function onLoadInit(mc)
   {
      var _loc4_;
      var _loc5_;
      var _loc6_;
      var _loc7_;
      var _loc8_;
      var _loc9_;
      var _loc10_;
      var _loc11_;
      var _loc12_;
      var _loc13_;
      var _loc14_;
      var _loc15_;
      var _loc16_;
      var _loc17_;
      var _loc18_;
      var _loc19_;
      var _loc20_;
      var _loc21_;
      if(this._aArgs.ornamento)
      {
         _loc4_ = mc._parent._parent._parent;
         if(mc.attachMovie("ornament_" + this._frameStart,"_ornamento",30))
         {
            mc._ornamento._y -= this._aArgs.medio;
            if(_global.API.kernel.OptionsManager.getOption("omega") && _global.omegaPerso > 0)
            {
               mc._ornamento.bottom._Omega._visible = true;
               mc._ornamento.bottom._lblOmega._visible = true;
               mc._ornamento.bottom._lblOmega.text = _global.omegaPerso;
            }
            else
            {
               mc._ornamento.bottom._Omega._visible = false;
               mc._ornamento.bottom._lblOmega._visible = false;
            }
            _loc4_._mcTxtBackground._y -= this._aArgs.medio;
            _loc5_ = 0.75;
            _loc6_ = 40;
            _loc7_ = 160;
            _loc8_ = _global.parseInt(this._aArgs.alto);
            _loc9_ = _global.parseInt(this._aArgs.ancho);
            _loc10_ = _loc9_ / _loc7_;
            _loc11_ = _loc8_ / _loc6_;
            _loc12_ = 1;
            if(_loc10_ > _loc5_ || _loc11_ > _loc5_)
            {
               _loc13_ = _loc7_ / _loc9_;
               _loc14_ = _loc6_ / _loc8_;
               _loc12_ = _loc13_;
               if(_loc14_ < _loc12_)
               {
                  _loc12_ = _loc14_;
               }
            }
            _loc15_ = 0;
            if(_loc10_ > _loc15_)
            {
               _loc15_ = _loc10_;
            }
            _loc11_ /= _loc10_;
            _loc16_ = mc._ornamento;
            _loc17_ = 80 + _loc16_.bg._x;
            _loc18_ = _loc16_.bg._y;
            _loc19_ = [_loc16_.bg,_loc16_.picto,_loc16_.top,_loc16_.left,_loc16_.right,_loc16_.bottom];
            for(var _loc22_ in _loc19_)
            {
               _loc19_[_loc22_]._x -= _loc17_;
               _loc19_[_loc22_]._y -= _loc18_;
            }
            _loc16_.bg._yscale = _loc11_ * 100;
            _loc16_.bottom._y *= _loc11_;
            _loc16_._xscale = _loc16_._yscale = _loc15_ * 100;
            _loc4_._xscale = _loc4_._yscale = _loc12_ * 100;
            _loc20_ = _loc4_.getBounds(_loc4_._parent._parent);
            _loc21_ = _loc20_.yMax + 20;
            _loc4_._y -= 30;
         }
         _loc4_._visible = true;
      }
      else
      {
         if(this._frameStart != undefined)
         {
            mc.gotoAndStop(this._frameStart);
         }
         mc._yscale = 100;
         mc._xscale = 100;
      }
      this.broadcastMessage("onLoadInit",mc,this._aArgs);
   }
   function onLoadError(mc, errorCode)
   {
      mc._visible = false;
      this.swf_mc._visible = false;
      this.broadcastMessage("onLoadError",mc,this._aArgs);
   }
}
