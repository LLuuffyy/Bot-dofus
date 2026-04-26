class ank.battlefield.mc.Sprite extends MovieClip
{
   var _ACTION;
   var _bHidden;
   var _mcBattlefield;
   var _mcCarried;
   var _mcChevauchor;
   var _mcChevauchorPos;
   var _mcGfx;
   var _mcStatesOverHead;
   var _mcXtraBack;
   var _mcXtraTop;
   var _mvlLoader;
   var _nDistance;
   var _nForcedXScale;
   var _nLastTimer;
   var _nOldCellNum;
   var _nRetryCount;
   var _oData;
   var _oSprites;
   var api;
   var dispatchEvent;
   var onEnterFrame;
   var stateID;
   var _nLastAlphaValue = 100;
   var _bGfxLoaded = false;
   var _bChevauchorGfxLoaded = false;
   var _bFallbackAttempted = false;
   static var WALK_SPEEDS = [0.07,0.06,0.06,0.06,0.07,0.06,0.06,0.06];
   static var MOUNT_SPEEDS = [0.23,0.2,0.2,0.2,0.23,0.2,0.2,0.2];
   static var RUN_SPEEDS = [0.17,0.15,0.15,0.15,0.17,0.15,0.15,0.15];
   function Sprite(b, sd, d)
   {
      super();
      mx.events.EventDispatcher.initialize(this);
      this.initialize(b,sd,d);
   }
   function set forcedXScale(nForcedXScale)
   {
      this._nForcedXScale = nForcedXScale;
      this._xscale = nForcedXScale;
   }
   function get gfx()
   {
      return this._mcGfx;
   }
   function get data()
   {
      return this._oData;
   }
   function set mcCarried(mc)
   {
      this._mcCarried = mc;
   }
   function set mcChevauchorPos(mc)
   {
      this._mcChevauchorPos = mc;
   }
   function set isHidden(b)
   {
      this.setHidden(b);
   }
   function get isHidden()
   {
      return this._bHidden;
   }
   function initialize(b, sd, d)
   {
      _global.GAC.addSprite(this,d);
      this._mcBattlefield = b;
      this._oSprites = sd;
      this._oData = d;
      this._mvlLoader = new MovieClipLoader();
      this._mvlLoader.addListener(this);
      this.setPosition(this._oData.cellNum);
      this.draw();
      this._ACTION = d;
      this.api = _global.API;
   }
   function draw()
   {
      this._mcGfx.removeMovieClip();
      this.createEmptyMovieClip("_mcGfx",20);
      this._visible = true;
      this.setHidden(this._bHidden);
      this._bGfxLoaded = false;
      this._bChevauchorGfxLoaded = false;
      this._bFallbackAttempted = false;
      this._nRetryCount = 0;
      this._mvlLoader.loadClip(this._oData.isMounting ? this._oData.mount.gfxFile : this._oData.gfxFile,this._mcGfx);
   }
   function clear()
   {
      this._mcBattlefield.mapHandler.getCellData(this._oData.cellNum).removeSpriteOnID(this._oData.id);
      this._mcGfx.removeMovieClip();
      if(this._mcStatesOverHead != undefined)
      {
         this._mcStatesOverHead.removeMovieClip();
      }
      this._oData.direction = 1;
      this.removeExtraClip();
      this._oData.isClear = true;
   }
   function select(bool)
   {
      var _loc3_ = {};
      if(bool)
      {
         _loc3_ = {ra:60,rb:102,ga:60,gb:102,ba:60,bb:102};
      }
      else
      {
         _loc3_ = {ra:100,rb:0,ga:100,gb:0,ba:100,bb:0};
      }
      this.setColorTransform(_loc3_);
   }
   function addExtraClip(sFile, nColor, bTop)
   {
      if(sFile == undefined)
      {
         return undefined;
      }
      if(bTop == undefined)
      {
         bTop = false;
      }
      this.removeExtraClip(bTop);
      var _loc5_;
      if(bTop)
      {
         _loc5_ = {};
         _loc5_.file = sFile;
         _loc5_.color = nColor;
         this._oData.xtraClipTopParams = _loc5_;
         if(!this._bGfxLoaded)
         {
            return undefined;
         }
      }
      var _loc6_ = !bTop ? this._mcXtraBack : this._mcXtraTop;
      var _loc7_;
      if(nColor != undefined)
      {
         _loc7_ = new Color(_loc6_);
         _loc7_.setRGB(nColor);
      }
      _loc6_.loadMovie(sFile);
   }
   function removeExtraClip(bTop)
   {
      switch(bTop)
      {
         case true:
            this._mcXtraTop.removeMovieClip();
            this.createEmptyMovieClip("_mcXtraTop",30);
            return undefined;
         case false:
            this._mcXtraBack.removeMovieClip();
            this.createEmptyMovieClip("_mcXtraBack",10);
            return undefined;
         default:
            this._mcXtraTop.removeMovieClip();
            this._mcXtraBack.removeMovieClip();
            this.createEmptyMovieClip("_mcXtraTop",30);
            this.createEmptyMovieClip("_mcXtraBack",10);
            return undefined;
      }
   }
   function setColorTransform(t)
   {
      var _loc3_ = new Color(this);
      _loc3_.setTransform(t);
   }
   function setNewCellNum(nCellNum)
   {
      this._oData.cellNum = Number(nCellNum);
   }
   function setDirection(nDir)
   {
      if(nDir == undefined)
      {
         nDir = this._oData.direction;
      }
      this._oData.direction = nDir;
      this.setAnim(this._oData.animation);
   }
   function setPosition(nCellNum)
   {
      this.updateMap(nCellNum,this._oData.isVisible);
      this.setDepth(nCellNum);
      if(nCellNum == undefined)
      {
         nCellNum = this._oData.cellNum;
      }
      else
      {
         this.setNewCellNum(nCellNum);
      }
      var _loc3_ = this._mcBattlefield.mapHandler.getCellData(nCellNum);
      var _loc4_ = this._mcBattlefield.mapHandler.getCellHeight(nCellNum);
      var _loc5_ = _loc4_ - Math.floor(_loc4_);
      this._x = _loc3_.x;
      this._y = _loc3_.y - _loc5_ * ank.battlefield.Constants.LEVEL_HEIGHT;
   }
   function setDepth(nCellNum)
   {
      if(nCellNum == undefined)
      {
         nCellNum = this._oData.cellNum;
      }
      var _loc3_ = ank.battlefield.utils.SpriteDepthFinder.getFreeDepthOnCell(this._mcBattlefield.mapHandler,this._oSprites,nCellNum,this._mcBattlefield.bGhostView);
      this.swapDepths(_loc3_);
      if(this._oData.hasCarriedChild())
      {
         this._oData.carriedChild.mc.setDepth(nCellNum);
      }
   }
   function setVisible(bool)
   {
      this._oData.isVisible = bool;
      this._visible = bool;
      this.updateMap(this._oData.cellNum,bool);
   }
   function setAlpha(value)
   {
      this._mcGfx._alpha = value;
   }
   function setHidden(b)
   {
      this._bHidden = b;
      if(this._bHidden)
      {
         this._mcGfx._x = this._mcGfx._y = -5000;
         this._mcGfx._visible = false;
      }
      else
      {
         this._mcGfx._x = this._mcGfx._y = 0;
         this._mcGfx._visible = true;
      }
   }
   function setGhostView(bool)
   {
      this.setDepth();
      if(bool)
      {
         this._nLastAlphaValue = this._mcGfx._alpha;
         this.setAlpha(ank.battlefield.Constants.GHOSTVIEW_SPRITE_ALPHA);
      }
      else
      {
         this.setAlpha(this._nLastAlphaValue);
      }
   }
   function moveToCell(seq, cellNum, bStop, sSpeedType, sAnimation, bForceAnimation)
   {
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
      if(cellNum != this._oData.cellNum)
      {
         _loc8_ = this._mcBattlefield.mapHandler.getCellData(this._oData.cellNum);
         _loc9_ = this._mcBattlefield.mapHandler.getCellData(cellNum);
         _loc10_ = _loc9_.x;
         _loc11_ = _loc9_.y;
         _loc12_ = 0.01;
         if(_loc9_.groundSlope != 1)
         {
            _loc11_ -= ank.battlefield.Constants.HALF_LEVEL_HEIGHT;
         }
         if(sAnimation.toLowerCase() != "static")
         {
            this._oData.direction = ank.battlefield.utils.Pathfinding.getDirectionFromCoordinates(_loc8_.x,_loc8_.rootY,_loc10_,_loc9_.rootY,true);
         }
         _loc13_ = this.api.electron.isWindowFocused;
         switch(sSpeedType)
         {
            case "slide":
               _loc14_ = 0.25;
               if(_loc13_)
               {
                  this.setAnim(sAnimation);
                  break;
               }
               this.setAnim("static");
               break;
            case "walk":
            default:
               _loc14_ = ank.battlefield.mc.Sprite.WALK_SPEEDS[this._oData.direction];
               if(_loc13_)
               {
                  this.setAnim(sAnimation != undefined ? sAnimation : "walk",undefined,bForceAnimation);
                  break;
               }
               this.setAnim("static");
               break;
            case "run":
               _loc14_ = this._oData.isMounting ? ank.battlefield.mc.Sprite.MOUNT_SPEEDS[this._oData.direction] : ank.battlefield.mc.Sprite.RUN_SPEEDS[this._oData.direction];
               if(_loc13_)
               {
                  this.setAnim(sAnimation != undefined ? sAnimation : "run",undefined,bForceAnimation);
                  break;
               }
               this.setAnim("static");
         }
         _loc14_ *= this._oData.speedModerator;
         if(_loc9_.groundLevel < _loc8_.groundLevel)
         {
            _loc14_ += _loc12_;
         }
         else if(_loc9_.groundLevel > _loc8_.groundLevel)
         {
            _loc14_ -= _loc12_;
         }
         else if(_loc8_.groundSlope != _loc9_.groundSlope)
         {
            if(_loc9_.groundSlope == 1)
            {
               _loc14_ += _loc12_;
            }
            else if(_loc8_.groundSlope == 1)
            {
               _loc14_ -= _loc12_;
            }
         }
         this._nDistance = Math.sqrt(Math.pow(this._x - _loc10_,2) + Math.pow(this._y - _loc11_,2));
         _loc15_ = Math.atan2(_loc11_ - this._y,_loc10_ - this._x);
         _loc16_ = Math.cos(_loc15_);
         _loc17_ = Math.sin(_loc15_);
         this._nLastTimer = getTimer();
         _loc18_ = Number(cellNum) > this._oData.cellNum;
         this.updateMap(cellNum,this._oData.isVisible,true);
         this.setNewCellNum(cellNum);
         this._oData.isInMove = true;
         this._oData.moveSpeedType = sSpeedType;
         this._oData.moveAnimation = sAnimation;
         if(this._oData.hasCarriedChild())
         {
            _loc19_ = this._oData.carriedChild;
            _loc20_ = _loc19_.mc;
            _loc20_.setDirection(this._oData.direction);
            _loc20_.updateMap(cellNum,_loc19_.isVisible);
            _loc20_.setNewCellNum(cellNum);
         }
         if(_loc18_)
         {
            this.setDepth(cellNum);
         }
         ank.utils.CyclicExecutor.getInstance().addFunction(this,this,this.basicMove,[_loc14_,_loc16_,_loc17_],this,this.basicMoveEnd,[seq,_loc10_,_loc11_,cellNum,bStop,sSpeedType == "slide",!_loc18_]);
      }
      else
      {
         seq.onActionEnd();
      }
   }
   function basicMove(speed, cosRot, sinRot)
   {
      var _loc5_ = getTimer() - this._nLastTimer;
      var _loc6_ = speed * (_loc5_ <= 125 ? _loc5_ : 125);
      this._x += _loc6_ * cosRot;
      this._y += _loc6_ * sinRot;
      this._nDistance -= _loc6_;
      this._nLastTimer = getTimer();
      if(this._nDistance <= _loc6_)
      {
         return false;
      }
      return true;
   }
   function basicMoveEnd(seq, xDest, yDest, cellNum, bStop, bSlide, bSetDepth)
   {
      if(this._nOldCellNum != undefined)
      {
         this._mcBattlefield.mapHandler.getCellData(this._nOldCellNum).removeSpriteOnID(this._oData.id);
         this._nOldCellNum = undefined;
      }
      if(bStop)
      {
         this._x = xDest;
         this._y = yDest;
         this._oData.isInMove = false;
         this.setAnim(this._oData.defaultAnimation);
         if(this.api.gfx.spriteHandler.isShowingMonstersTooltip && this.data instanceof dofus.datacenter.MonsterGroup)
         {
            this._rollOver(true);
         }
      }
      if(bSetDepth)
      {
         this.setDepth(cellNum);
      }
      seq.onActionEnd();
   }
   function saveLastAnimation(sAnim)
   {
      if(!this._oData.isMounting)
      {
         this._mcGfx.mcAnim.lastAnimation = sAnim;
      }
      else
      {
         this._mcChevauchor.mcAnim.lastAnimation = sAnim;
         this._mcGfx.mcAnimFront.lastAnimation = sAnim;
         this._mcGfx.mcAnimBack.lastAnimation = sAnim;
      }
   }
   function setAnimTimer(anim, bLoop, bForced, nTimer)
   {
      this.setAnim(anim,bLoop,bForced);
      if(_global.isNaN(Number(nTimer)))
      {
         return undefined;
      }
      if(nTimer < 1)
      {
         return undefined;
      }
      ank.utils.Timer.setTimer(this,"battlefield",this,this.setAnim,nTimer,[this._oData.defaultAnimation]);
   }
   static function getDirNumByChar(sDir)
   {
      var _loc2_;
      switch(sDir)
      {
         case "S":
            _loc2_ = 0;
            break;
         case "R":
            _loc2_ = 1;
            break;
         case "F":
            _loc2_ = 2;
            break;
         case "L":
            _loc2_ = 5;
            break;
         case "B":
            _loc2_ = 6;
            break;
         default:
            _loc2_ = -1;
      }
      return _loc2_;
   }
   function setAnim(anim, bLoop, bForced)
   {
      var _loc5_;
      if(this.api.datacenter.Game.isRunning)
      {
         _loc5_ = this._oData.sequencer.getCurrentAction();
         if(_loc5_ != undefined && (_loc5_.object == this && (_loc5_.fn == this.setAnim && (_loc5_.waitEnd && (!_loc5_.forceTimeout && _loc5_.functionApplied)))))
         {
            return undefined;
         }
      }
      if(anim == undefined)
      {
         anim = this._oData.defaultAnimation;
      }
      anim = String(anim).toLowerCase();
      if(bLoop == undefined)
      {
         bLoop = false;
      }
      if(bForced == undefined)
      {
         bForced = false;
      }
      var _loc6_ = this._oData.noFlip;
      this._oData.bAnimLoop = bLoop;
      var mc = this._mcGfx;
      var _loc7_ = "";
      if(this._oData.hasCarriedChild())
      {
         _loc7_ += "_C";
      }
      var sFullAnim;
      var sDir;
      var nScale;
      var _loc8_;
      switch(this._oData.direction)
      {
         case 0:
            sDir = "S";
            sFullAnim = anim + _loc7_ + sDir;
            _loc8_ = "staticR";
            nScale = 100;
            break;
         case 1:
            sDir = "R";
            sFullAnim = anim + _loc7_ + sDir;
            _loc8_ = "staticR";
            nScale = 100;
            break;
         case 2:
            sDir = "F";
            sFullAnim = anim + _loc7_ + sDir;
            _loc8_ = "staticR";
            nScale = 100;
            break;
         case 3:
            sDir = "R";
            sFullAnim = anim + _loc7_ + sDir;
            _loc8_ = "staticR";
            if(!_loc6_)
            {
               nScale = -100;
            }
            break;
         case 4:
            sDir = "S";
            sFullAnim = anim + _loc7_ + sDir;
            _loc8_ = "staticL";
            if(!_loc6_)
            {
               nScale = -100;
            }
            break;
         case 5:
            sDir = "L";
            sFullAnim = anim + _loc7_ + sDir;
            _loc8_ = "staticL";
            nScale = 100;
            break;
         case 6:
            sDir = "B";
            sFullAnim = anim + _loc7_ + sDir;
            _loc8_ = "staticL";
            nScale = 100;
            break;
         case 7:
            sDir = "L";
            sFullAnim = anim + _loc7_ + sDir;
            _loc8_ = "staticL";
            if(!_loc6_)
            {
               nScale = -100;
            }
      }
      if(this._nForcedXScale != undefined)
      {
         nScale = this._nForcedXScale;
      }
      var _loc9_ = this._oData.fullAnimation;
      var sOldAnim = this._oData.animation;
      this._oData.animation = anim;
      this._oData.fullAnimation = sFullAnim;
      if(this._oData.xtraClipTopAnimations != undefined)
      {
         if(this._oData.xtraClipTopAnimations[sFullAnim])
         {
            this.addExtraClip(this._oData.xtraClipTopParams.file,this._oData.xtraClipTopParams.color,true);
         }
         else if(this._mcXtraTop != undefined)
         {
            this.removeExtraClip(true);
         }
      }
      var _loc10_;
      if(bForced || sFullAnim != _loc9_)
      {
         var ref = this;
         _loc10_ = mc.createEmptyMovieClip("mcAnimLoad",31);
         _loc10_._visible = false;
         if(!this._oData.isMounting)
         {
            _loc10_.onEnterFrame = function()
            {
               ref._xscale = nScale;
               var _loc2_ = mc.attachMovie(sFullAnim,"mcAnim",10,{lastAnimation:sOldAnim});
               if(_loc2_ == undefined)
               {
                  _loc2_ = mc.attachMovie("static" + sDir,"mcAnim",10,{lastAnimation:sOldAnim});
               }
               if(_loc2_ != undefined && ank.battlefield.Battlefield.useCacheAsBitmapOnStaticAnim)
               {
                  _loc2_.cacheAsBitmap = _loc2_._totalframes == 1;
               }
               this.removeMovieClip();
               delete this.onEnterFrame;
            };
         }
         else
         {
            _loc10_._visible = false;
            _loc10_.onEnterFrame = function()
            {
               ref._xscale = nScale;
               var _loc2_ = ref._mcChevauchor.attachMovie(sFullAnim,"mcAnim",1,{lastAnimation:sOldAnim});
               if(_loc2_ == undefined)
               {
                  _loc2_ = ref._mcChevauchor.attachMovie("static" + sDir,"mcAnim",1,{lastAnimation:sOldAnim});
               }
               if(_loc2_ == undefined)
               {
                  ref._mcChevauchor.mcAnim.removeMovieClip();
               }
               else if(ank.battlefield.Battlefield.useCacheAsBitmapOnStaticAnim)
               {
                  _loc2_.cacheAsBitmap = _loc2_._totalframes == 1;
               }
               _loc2_ = mc.attachMovie(sFullAnim + "_Front","mcAnimFront",30,{lastAnimation:sOldAnim});
               if(_loc2_ == undefined)
               {
                  _loc2_ = mc.attachMovie("static" + sDir + "_Front","mcAnimFront",30,{lastAnimation:sOldAnim});
               }
               if(_loc2_ == undefined)
               {
                  mc.mcAnimFront.removeMovieClip();
               }
               else if(ank.battlefield.Battlefield.useCacheAsBitmapOnStaticAnim)
               {
                  _loc2_.cacheAsBitmap = _loc2_._totalframes == 1;
               }
               _loc2_ = mc.attachMovie(sFullAnim + "_Back","mcAnimBack",10,{lastAnimation:sOldAnim});
               if(_loc2_ == undefined)
               {
                  _loc2_ = mc.attachMovie("static" + sDir + "_Back","mcAnimBack",10,{lastAnimation:sOldAnim});
               }
               if(_loc2_ == undefined)
               {
                  mc.mcAnimBack.removeMovieClip();
               }
               else if(ank.battlefield.Battlefield.useCacheAsBitmapOnStaticAnim)
               {
                  _loc2_.cacheAsBitmap = _loc2_._totalframes == 1;
               }
               if(ref._oData.isMounting)
               {
                  ank.utils.CyclicExecutor.getInstance().addFunction(ref,ref,ref.updateChevauchorPosition);
               }
               this.removeMovieClip();
               delete this.onEnterFrame;
            };
         }
      }
      else
      {
         this._xscale = nScale;
         if(this._oData.isMounting)
         {
            ank.utils.CyclicExecutor.getInstance().addFunction(this,this,this.updateChevauchorPosition);
         }
      }
      if(this._oData.hasCarriedChild())
      {
         ank.utils.CyclicExecutor.getInstance().addFunction(this,this,this.updateCarriedPosition);
      }
   }
   function updateCarriedPosition()
   {
      var _loc2_;
      if(this._oData.hasCarriedChild())
      {
         if(this._mcCarried != undefined)
         {
            _loc2_ = {x:this._mcCarried._x,y:this._mcCarried._y};
            this._mcCarried._parent.localToGlobal(_loc2_);
            this._mcBattlefield.container.globalToLocal(_loc2_);
            this._oData.carriedChild.mc._x = _loc2_.x;
            this._oData.carriedChild.mc._y = _loc2_.y;
         }
      }
      return this._oData.animation != "static" || this._oData.isInMove;
   }
   function updateChevauchorPosition()
   {
      var _loc2_;
      if(this._oData.isMounting)
      {
         if(this._mcChevauchorPos != undefined)
         {
            _loc2_ = {x:this._mcChevauchorPos._x,y:this._mcChevauchorPos._y};
            this._mcChevauchorPos._parent.localToGlobal(_loc2_);
            this._mcGfx.globalToLocal(_loc2_);
            this._mcChevauchor._x = _loc2_.x;
            this._mcChevauchor._y = _loc2_.y;
            this._mcChevauchor._rotation = this._mcChevauchorPos._rotation;
            this._mcChevauchor._xscale = this._mcChevauchorPos._xscale;
            this._mcChevauchor._yscale = this._mcChevauchorPos._yscale;
         }
      }
      return this._oData.animation != "static" || this._oData.isInMove;
   }
   function updateMap(nCellNum, bVisible, bDontRemoveAllSpriteOn)
   {
      var _loc5_ = this._mcBattlefield.mapHandler.getCellData(nCellNum);
      if(_loc5_ == undefined)
      {
         if(bVisible)
         {
            this._mcBattlefield.mapHandler.getCellData(this._oData.cellNum).addSpriteOnID(this._oData.id);
         }
         else
         {
            this._mcBattlefield.mapHandler.getCellData(this._oData.cellNum).removeSpriteOnID(this._oData.id);
         }
         return undefined;
      }
      if(bDontRemoveAllSpriteOn != true)
      {
         this._mcBattlefield.mapHandler.getCellData(this._oData.cellNum).removeSpriteOnID(this._oData.id);
      }
      else
      {
         this._nOldCellNum = this._oData.cellNum;
      }
      if(bVisible)
      {
         _loc5_.addSpriteOnID(this._oData.id);
      }
   }
   function setScale(nScaleX, nScaleY)
   {
      this._mcGfx._xscale = nScaleX;
      this._mcGfx._yscale = nScaleY == undefined ? nScaleX : nScaleY;
   }
   function onLoadInit(mc)
   {
      var _ref = this;
      this.onEnterFrame = function()
      {
         if(!this._bGfxLoaded)
         {
            this._bGfxLoaded = true;
            if(this._oData.isMounting)
            {
               this._mcChevauchor = this._mcGfx.createEmptyMovieClip("chevauchor",20);
               this._mvlLoader.loadClip(this._oData.mount.chevauchorGfxFile,this._mcChevauchor);
            }
         }
         else
         {
            this._bChevauchorGfxLoaded = true;
         }
         if(this._bGfxLoaded && (!this._oData.isMounting || this._bChevauchorGfxLoaded))
         {
            if(_global.isNaN(Number(this._oData.startAnimationTimer)))
            {
               this.setAnim(this._oData.startAnimation,undefined,true);
            }
            else
            {
               this.setAnimTimer(this._oData.startAnimation,false,false,this._oData.startAnimationTimer);
            }
            delete this.onEnterFrame;
            if(_ref._oData != undefined && _ref._oData.id != undefined)
            {
               if(_global._hardAuraReapplyCallback != undefined)
               {
                  _global._hardAuraReapplyCallback(_ref._oData.id);
               }
            }
            _ref.updateStatesOverHead();
            if(_ref._oData != undefined)
            {
               _ref._oData.addEventListener("statesChanged",_ref);
            }
         }
      };
      this.dispatchEvent({type:"onLoadInit",clip:this});
   }
   function onLoadError(mc, errorCode)
   {
      if(!this._nRetryCount)
      {
         this._nRetryCount = 0;
      }
      var _loc4_;
      if(this._nRetryCount < 2)
      {
         this._nRetryCount += 1;
         _loc4_ = this._oData.isMounting ? this._oData.mount.gfxFile : this._oData.gfxFile;
         this._mvlLoader.loadClip(_loc4_ + "?r=" + getTimer(),this._mcGfx);
      }
      else if(!this._bFallbackAttempted)
      {
         this._bFallbackAttempted = true;
         this._mvlLoader.loadClip("clips/sprites/0.swf?r=" + getTimer(),this._mcGfx);
      }
      else
      {
         this._mcGfx._visible = false;
         this._visible = false;
      }
   }
   function updateStatesOverHead()
   {
      if(this._mcStatesOverHead != undefined)
      {
         this._mcStatesOverHead.removeMovieClip();
      }
      if(!this._bGfxLoaded)
      {
         return undefined;
      }
      if(this.api == undefined || this.api.kernel == undefined)
      {
         return undefined;
      }
      if(!this.api.kernel.OptionsManager.getOption("StatesOverHead"))
      {
         return undefined;
      }
      var _loc2_ = this._oData.states;
      if(_loc2_ == undefined)
      {
         return undefined;
      }
      var _loc3_ = [];
      for(var _loc4_ in _loc2_)
      {
         if(_loc2_[_loc4_] == true)
         {
            _loc3_.push(Number(_loc4_));
         }
      }
      if(_loc3_.length == 0)
      {
         return undefined;
      }
      this._mcStatesOverHead = this.createEmptyMovieClip("_mcStatesOverHead",40);
      this._mcStatesOverHead._yscale = 50;
      this._mcStatesOverHead._y = -60;
      var _loc5_ = this;
      this._mcStatesOverHead.onEnterFrame = function()
      {
         this._xscale = _loc5_._xscale < 0 ? -50 : 50;
      };
      this._mcStatesOverHead._xscale = this._xscale < 0 ? -50 : 50;
      var _loc5_ = _loc3_.length;
      var _loc6_ = _loc5_ * 16;
      var _loc7_ = (- _loc6_) / 2;
      var _loc8_ = 0;
      var _loc9_;
      var _loc10_;
      var _loc11_;
      while(_loc8_ < _loc5_)
      {
         _loc9_ = this._mcStatesOverHead.createEmptyMovieClip("state_" + _loc8_,_loc8_ + 1);
         _loc9_._x = _loc7_ + _loc8_ * 16;
         _loc10_ = _loc3_[_loc8_];
         _loc11_ = new MovieClipLoader();
         _loc11_.addListener({stateID:_loc10_,onLoadInit:function(target)
         {
            target.attachMovie("State_" + this.stateID,"_mcIcon",1);
         }});
         _loc11_.loadClip("clips/statesicons.swf",_loc9_);
         _loc8_ += 1;
      }
   }
   function statesChanged(oEvent)
   {
      this.updateStatesOverHead();
   }
   function _release(Void)
   {
      this._mcBattlefield.onSpriteRelease(this);
   }
   function _rollOver(bFakeEvent)
   {
      this._mcBattlefield.onSpriteRollOver(this,bFakeEvent);
   }
   function _rollOut(bFakeEvent)
   {
      this._mcBattlefield.onSpriteRollOut(this,bFakeEvent);
   }
}
