class ank.battlefield.datacenter.Sprite extends Object
{
   var _aAccessories;
   var _bClear;
   var _bInMove;
   var _bVisible;
   var _eoLinkedChilds;
   var _nCellNum;
   var _nColor1;
   var _nColor2;
   var _nColor3;
   var _nCreationInstant;
   var _nDirection;
   var _nStartAnimationTimer;
   var _oCarriedChild;
   var _oCarriedParent;
   var _oLinkedParent;
   var _oMount;
   var _oSequencer;
   var _sGfxFile;
   var _sGfxFileName;
   var _sMoveAnimation;
   var _sMoveSpeedType;
   var _states;
   var clipClass;
   var dispatchEvent;
   var id;
   var mc;
   static var ANGELS_OF_THE_WORLD_SPRITE_ID = "999";
   static var ANGELS_OF_THE_WORLD_REPLACEMENT_SPRITE_ID = "8023";
   var allowGhostMode = true;
   var bAnimLoop = false;
   var _nChildIndex = -1;
   var _nFutureCellNum = -1;
   var _sDefaultAnimation = "static";
   var _sStartAnimation = "static";
   var _nSpeedModerator = 2.3;
   var _bHidden = false;
   var _bAllDirections = true;
   var _bForceWalk = false;
   var _bForceRun = false;
   var _bNoFlip = false;
   var _bIsPendingClearing = false;
   var _bUncarryingSprite = false;
   var bInCreaturesMode = false;
   var _bIsInvisibleInFight = false;
   function Sprite(nID, fClipClass, sGfxFile, nCellNum, nDir)
   {
      super();
      this.initialize(nID,fClipClass,sGfxFile,nCellNum,nDir);
   }
   function initialize(sID, fClipClass, sGfxFile, nCellNum, nDir)
   {
      this.id = sID;
      this.clipClass = fClipClass;
      this._sGfxFile = sGfxFile;
      this.refreshGfxFileName();
      this._nCellNum = Number(nCellNum);
      this._nDirection = nDir != undefined ? Number(nDir) : 1;
      this._oSequencer = new ank.utils.Sequencer(1000);
      this._bInMove = false;
      this._bVisible = true;
      this._bClear = false;
      this._eoLinkedChilds = new ank.utils.ExtendedObject();
      mx.events.EventDispatcher.initialize(this);
      this._states = {};
      this._nCreationInstant = getTimer();
   }
   function refreshGfxFileName()
   {
      var _loc2_ = this._sGfxFile.split(".")[0].split("/");
      this._sGfxFileName = _loc2_[_loc2_.length - 1];
   }
   function isLocalPlayer(api)
   {
      return this.id == api.datacenter.Player.ID;
   }
   function set uncarryingSprite(bUncarryingSprite)
   {
      this._bUncarryingSprite = bUncarryingSprite;
   }
   function get uncarryingSprite()
   {
      return this._bUncarryingSprite;
   }
   function get hasChilds()
   {
      return this._eoLinkedChilds.getLength() != 0;
   }
   function get hasParent()
   {
      return this.linkedParent != undefined;
   }
   function get childIndex()
   {
      return this._nChildIndex;
   }
   function set childIndex(nChildIndex)
   {
      this._nChildIndex = nChildIndex;
   }
   function get linkedChilds()
   {
      return this._eoLinkedChilds;
   }
   function get linkedParent()
   {
      return this._oLinkedParent;
   }
   function set linkedParent(oLinkedParent)
   {
      this._oLinkedParent = oLinkedParent;
   }
   function hasCarriedChild()
   {
      return this._oCarriedChild != undefined;
   }
   function hasCarriedParent()
   {
      return this._oCarriedParent != undefined;
   }
   function get carriedChild()
   {
      return this._oCarriedChild;
   }
   function set carriedChild(o)
   {
      this._oCarriedChild = o;
   }
   function get carriedParent()
   {
      return this._oCarriedParent;
   }
   function set carriedParent(o)
   {
      this._oCarriedParent = o;
   }
   function get creationInstant()
   {
      return this._nCreationInstant;
   }
   function get gfxFile()
   {
      return this._sGfxFile;
   }
   function set gfxFile(sGfxFile)
   {
      this.dispatchEvent({type:"gfxFileChanged",value:sGfxFile});
      this._sGfxFile = sGfxFile;
      this.refreshGfxFileName();
   }
   function get gfxFileName()
   {
      return this._sGfxFileName;
   }
   function get defaultAnimation()
   {
      return this._sDefaultAnimation;
   }
   function set defaultAnimation(value)
   {
      this._sDefaultAnimation = value;
   }
   function get startAnimation()
   {
      return this._sStartAnimation;
   }
   function set startAnimation(value)
   {
      this._sStartAnimation = value;
   }
   function get startAnimationTimer()
   {
      return this._nStartAnimationTimer;
   }
   function set startAnimationTimer(value)
   {
      this._nStartAnimationTimer = value;
   }
   function get speedModerator()
   {
      return this._nSpeedModerator;
   }
   function set speedModerator(value)
   {
      this._nSpeedModerator = Number(value);
   }
   function get isVisible()
   {
      return this._bVisible;
   }
   function set isVisible(value)
   {
      this._bVisible = value;
   }
   function get isInvisibleInFight()
   {
      return this._bIsInvisibleInFight;
   }
   function set isInvisibleInFight(bIsInvisibleInFight)
   {
      this._bIsInvisibleInFight = bIsInvisibleInFight;
   }
   function setInvisibleInFight(bIsInvisibleInFight)
   {
      this._bIsInvisibleInFight = bIsInvisibleInFight;
   }
   function get isHidden(Void)
   {
      return this._bHidden;
   }
   function set isHidden(value)
   {
      this.mc.isHidden = this._bHidden = value;
   }
   function get isInMove()
   {
      return this._bInMove;
   }
   function set isInMove(value)
   {
      if(!value)
      {
         this._nFutureCellNum = -1;
         this._sMoveSpeedType = undefined;
         this._sMoveAnimation = undefined;
      }
      this._bInMove = value;
      if(this.hasCarriedChild())
      {
         this.carriedChild.isInMove = value;
      }
   }
   function get moveSpeedType()
   {
      return this._sMoveSpeedType;
   }
   function set moveSpeedType(sMoveSpeedType)
   {
      this._sMoveSpeedType = sMoveSpeedType;
   }
   function get moveAnimation()
   {
      return this._sMoveAnimation;
   }
   function set moveAnimation(sMoveAnimation)
   {
      this._sMoveAnimation = sMoveAnimation;
   }
   function get isClear()
   {
      return this._bClear;
   }
   function set isClear(value)
   {
      this._bClear = value;
   }
   function get cellNum()
   {
      return this._nCellNum;
   }
   function set cellNum(value)
   {
      this._nCellNum = Number(value);
   }
   function get futureCellNum()
   {
      return this._nFutureCellNum;
   }
   function set futureCellNum(nFutureCellNum)
   {
      this._nFutureCellNum = nFutureCellNum;
   }
   function get direction()
   {
      return this._nDirection;
   }
   function set direction(value)
   {
      this._nDirection = Number(value);
   }
   function get color1()
   {
      return this._nColor1;
   }
   function set color1(value)
   {
      this._nColor1 = Number(value);
   }
   function get color2()
   {
      return this._nColor2;
   }
   function set color2(value)
   {
      this._nColor2 = Number(value);
   }
   function get color3()
   {
      return this._nColor3;
   }
   function set color3(value)
   {
      this._nColor3 = Number(value);
   }
   function get accessories()
   {
      return this._aAccessories;
   }
   function set accessories(value)
   {
      this.dispatchEvent({type:"accessoriesChanged",value:value});
      this._aAccessories = value;
   }
   function get sequencer()
   {
      return this._oSequencer;
   }
   function set sequencer(value)
   {
      this._oSequencer = value;
   }
   function get allDirections()
   {
      return this._bAllDirections;
   }
   function set allDirections(bAllDirections)
   {
      this._bAllDirections = bAllDirections;
   }
   function get forceWalk()
   {
      return this._bForceWalk;
   }
   function set forceWalk(bForceWalk)
   {
      this._bForceWalk = bForceWalk;
   }
   function get forceRun()
   {
      return this._bForceRun;
   }
   function set forceRun(bForceRun)
   {
      this._bForceRun = bForceRun;
   }
   function get noFlip()
   {
      return this._bNoFlip;
   }
   function set noFlip(bNoFlip)
   {
      this._bNoFlip = bNoFlip;
   }
   function get mount()
   {
      return this._oMount;
   }
   function set mount(v)
   {
      this._oMount = v;
   }
   function get isMounting()
   {
      return this._oMount != undefined;
   }
   function get isPendingClearing()
   {
      return this._bIsPendingClearing;
   }
   function set isPendingClearing(bIsPendingClearing)
   {
      this._bIsPendingClearing = bIsPendingClearing;
   }
   function get states()
   {
      return this._states;
   }
   function isInState(stateID)
   {
      return this._states[stateID] == true;
   }
   function setState(api, stateID, bActivate)
   {
      this._states[stateID] = bActivate;
      if(bActivate)
      {
         dofus.datacenter.States.onStateAdded(api,this,stateID);
      }
      else
      {
         dofus.datacenter.States.onStateRemoved(api,this,stateID);
      }
      this.dispatchEvent({type:"statesChanged",value:this._states});
   }
}
