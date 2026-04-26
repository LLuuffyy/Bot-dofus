class ank.gapi.controls.Container extends ank.gapi.core.UIBasicComponent
{
   var __height;
   var __updateLockBorder;
   var __width;
   var _bEnabled;
   var _bInitialized;
   var _lblText;
   var _ldrContent;
   var _mcBg;
   var _mcHighlight;
   var _mcInteraction;
   var _mcLabelBackground;
   var _nId;
   var _oContentData;
   var _oTempVars;
   var _parent;
   var _sBorder;
   var _sLabel;
   var addToQueue;
   var attachMovie;
   var createEmptyMovieClip;
   var dispatchEvent;
   var drawRoundRect;
   var getStyle;
   var initialized;
   static var CLASS_NAME = "Container";
   var _sBackground = "DefaultBackground";
   var _sHighlight = "DefaultHighlight";
   var _bDragAndDrop = true;
   var _bShowLabel = false;
   var _bSelected = false;
   var _nMargin = 2;
   var _bHighlightFront = true;
   function Container()
   {
      super();
   }
   function get tempVars()
   {
      return this._oTempVars;
   }
   function set tempVars(oTempVars)
   {
      this._oTempVars = oTempVars;
   }
   function set contentPath(sContentPath)
   {
      this.addToQueue({object:this,method:function(sPath)
      {
         this._ldrContent.contentPath = sPath;
      },params:[sContentPath]});
   }
   function set forceReload(bReload)
   {
      this._ldrContent.forceReload = bReload;
   }
   function get contentPath()
   {
      return this._ldrContent.contentPath;
   }
   function set contentData(oContentData)
   {
      this._oContentData = oContentData;
      if(this._oContentData.forceReloadOnContainer != undefined)
      {
         this._ldrContent.forceReload = this._oContentData.forceReloadOnContainer;
      }
      if(this._oContentData.params != undefined)
      {
         this._ldrContent.contentParams = this._oContentData.params;
      }
      var _loc4_;
      if(this._oContentData.iconFile != undefined)
      {
         this.contentPath = this._oContentData.iconFile;
         _loc4_ = dofus.datacenter.Item(this._oContentData);
         this._ldrContent.holder.filters = undefined;
         if(_global.API.datacenter.Basics.aks_current_server.isTemporis())
         {
            if(_loc4_.realUnicId >= dofus.Constants.REFFINED_ITEM.minimumID)
            {
               _loc4_.addGlowOnItemIcon(this._ldrContent.holder,dofus.Constants.REFFINED_ITEM.color,dofus.Constants.REFFINED_ITEM.alpha,dofus.Constants.REFFINED_ITEM.blur,dofus.Constants.REFFINED_ITEM.intensity);
            }
            else if(_loc4_.realUnicId >= dofus.Constants.IMPROVED_ITEM.minimumID)
            {
               _loc4_.addGlowOnItemIcon(this._ldrContent.holder,dofus.Constants.IMPROVED_ITEM.color,dofus.Constants.IMPROVED_ITEM.alpha,dofus.Constants.IMPROVED_ITEM.blur,dofus.Constants.IMPROVED_ITEM.intensity);
            }
         }
      }
      else
      {
         this.contentPath = "";
      }
      if(this._oContentData.label != undefined)
      {
         if(this.label != this._oContentData.label)
         {
            this.label = this._oContentData.label;
         }
      }
      else
      {
         this.label = "";
      }
      this.__updateLockBorder();
   }
   function get contentData()
   {
      return this._oContentData;
   }
   function get contentLoaded()
   {
      return this._ldrContent.loaded;
   }
   function get content()
   {
      return this._ldrContent.content;
   }
   function get holder()
   {
      return this._ldrContent.holder;
   }
   function set selected(bSelected)
   {
      this._bSelected = bSelected;
      this.addToQueue({object:this,method:function()
      {
         this._mcHighlight._visible = bSelected;
      }});
   }
   function get selected()
   {
      return this._bSelected;
   }
   function set backgroundRenderer(sBackground)
   {
      if(sBackground.length == 0 || sBackground == undefined)
      {
         return;
      }
      this._sBackground = sBackground;
      this.attachBackground();
      if(this._bInitialized)
      {
         this.size();
      }
   }
   function set borderRenderer(sBorder)
   {
      if(sBorder.length == 0 || sBorder == undefined)
      {
         return;
      }
      this._sBorder = sBorder;
      this.attachBorder();
      if(this._bInitialized)
      {
         this.size();
      }
   }
   function set highlightRenderer(sHighlight)
   {
      if(sHighlight.length == 0 || sHighlight == undefined)
      {
         return;
      }
      this._sHighlight = sHighlight;
      this.attachHighlight();
      if(this._bInitialized)
      {
         this.size();
      }
   }
   function set dragAndDrop(bDragAndDrop)
   {
      if(bDragAndDrop == undefined)
      {
         return;
      }
      this._bDragAndDrop = bDragAndDrop;
      if(this._bInitialized)
      {
         this.setEnabled();
      }
   }
   function get dragAndDrop()
   {
      return this._bDragAndDrop;
   }
   function set showLabel(bShowLabel)
   {
      if(bShowLabel == undefined)
      {
         return;
      }
      this._bShowLabel = bShowLabel;
      if(bShowLabel)
      {
         if(this._sLabel != undefined)
         {
            if(this._lblText == undefined)
            {
               this.attachMovie("Label","_lblText",30,{_width:this.__width,_height:this.__height,styleName:this.getStyle().labelstyle});
            }
            this.addToQueue({object:this,method:this.setLabel,params:[this._sLabel]});
         }
      }
      else
      {
         this._lblText.removeMovieClip();
         this._mcLabelBackground.clear();
      }
   }
   function get showLabel()
   {
      return this._bShowLabel;
   }
   function set label(sLabel)
   {
      this._sLabel = sLabel;
      if(this._bShowLabel)
      {
         if(this._lblText == undefined)
         {
            this.attachMovie("Label","_lblText",30,{_width:this.__width,_height:this.__height,styleName:this.getStyle().labelstyle});
         }
         this.addToQueue({object:this,method:this.setLabel,params:[sLabel]});
      }
   }
   function get label()
   {
      return this._sLabel;
   }
   function set margin(nMargin)
   {
      nMargin = Number(nMargin);
      if(_global.isNaN(nMargin))
      {
         return;
      }
      this._nMargin = nMargin;
      if(this.initialized)
      {
         this._ldrContent.move(this._nMargin,this._nMargin);
      }
   }
   function get margin()
   {
      return this._nMargin;
   }
   function set highlightFront(bHighlightFront)
   {
      this._bHighlightFront = bHighlightFront;
      if(!bHighlightFront && this._mcHighlight != undefined)
      {
         this._mcHighlight.swapDepths(1);
      }
   }
   function get highlightFront()
   {
      return this._bHighlightFront;
   }
   function set id(nId)
   {
      this._nId = nId;
   }
   function get id()
   {
      return this._nId;
   }
   function forceNextLoad()
   {
      this._ldrContent.forceNextLoad();
   }
   function emulateClick()
   {
      this.dispatchEvent({type:"click"});
   }
   function sizeContent()
   {
      this._ldrContent.size();
   }
   function init()
   {
      super.init(false,ank.gapi.controls.Container.CLASS_NAME);
   }
   function createChildren()
   {
      this.createEmptyMovieClip("_mcInteraction",0);
      this.drawRoundRect(this._mcInteraction,0,0,1,1,0,0,0);
      this._mcInteraction.trackAsMenu = true;
      this.attachMovie("GAPILoader","_ldrContent",20,{scaleContent:true});
      this._ldrContent.move(this._nMargin,this._nMargin);
      this._ldrContent.addEventListener("complete",this);
      this._ldrContent.addEventListener("initialization",this);
      this.createEmptyMovieClip("_mcLabelBackground",29);
   }
   function complete()
   {
      this.dispatchEvent({type:"onContentLoaded",content:this.content,target:this});
   }
   function initialization()
   {
      this.dispatchEvent({type:"onContentInitialized",content:this.content,target:this});
   }
   function size()
   {
      super.size();
      if(this._bInitialized)
      {
         this.arrange();
      }
   }
   function arrange()
   {
      this._mcInteraction._width = this.__width;
      this._mcInteraction._height = this.__height;
      this._ldrContent.setSize(this.__width - this._nMargin * 2,this.__height - this._nMargin * 2);
      this._mcBg.setSize(this.__width,this.__height);
      this._mcHighlight.setSize(this.__width,this.__height);
      this._lblText.setSize(this.__width,this.__height);
   }
   function draw()
   {
      var _loc2_ = this.getStyle();
      this._mcBg.styleName = _loc2_.backgroundstyle;
      this._lblText.styleName = _loc2_.labelstyle;
   }
   function setEnabled()
   {
      if(this._bEnabled)
      {
         this._mcInteraction.onRelease = function()
         {
            if(this._parent._sLastMouseAction == "DragOver")
            {
               this._parent.dispatchEvent({type:"drop"});
            }
            else if(getTimer() - this._parent._nLastClickTime < ank.gapi.Gapi.DBLCLICK_DELAY)
            {
               ank.utils.Timer.removeTimer(this._parent,"container");
               this._parent.dispatchEvent({type:"dblClick"});
            }
            else
            {
               ank.utils.Timer.setTimer(this._parent,"container",this._parent,this._parent.dispatchEvent,ank.gapi.Gapi.DBLCLICK_DELAY,[{type:"click"}]);
            }
            this._parent._sLastMouseAction = "Release";
            this._parent._nLastClickTime = getTimer();
         };
         this._mcInteraction.onPress = function()
         {
            this._parent._sLastMouseAction = "Press";
         };
         this._mcInteraction.onRollOver = function()
         {
            this._parent._mcHighlight._visible = true;
            this._parent._sLastMouseAction = "RollOver";
            this._parent.dispatchEvent({type:"over"});
         };
         this._mcInteraction.onRollOut = function()
         {
            if(!this._parent._bSelected)
            {
               this._parent._mcHighlight._visible = false;
            }
            this._parent._sLastMouseAction = "RollOver";
            this._parent.dispatchEvent({type:"out"});
         };
         if(this._bDragAndDrop)
         {
            this._mcInteraction.onDragOver = function()
            {
               this._parent._mcHighlight._visible = true;
               this._parent._sLastMouseAction = "DragOver";
               this._parent.dispatchEvent({type:"over"});
            };
            this._mcInteraction.onDragOut = function()
            {
               if(!this._parent._bSelected)
               {
                  this._parent._mcHighlight._visible = false;
               }
               if(this._parent._sLastMouseAction == "Press")
               {
                  this._parent.dispatchEvent({type:"drag"});
               }
               this._parent._sLastMouseAction = "DragOut";
               this._parent.dispatchEvent({type:"out"});
            };
         }
      }
      else
      {
         delete this._mcInteraction.onRelease;
         delete this._mcInteraction.onPress;
         delete this._mcInteraction.onRollOver;
         delete this._mcInteraction.onRollOut;
         delete this._mcInteraction.onDragOver;
         delete this._mcInteraction.onDragOut;
      }
   }
   function attachBackground()
   {
      this.attachMovie(this._sBackground,"_mcBg",10);
   }
   function attachBorder()
   {
      this.attachMovie(this._sBorder,"_mcBorder",90);
   }
   function attachHighlight()
   {
      this.attachMovie(this._sHighlight,"_mcHighlight",!this._bHighlightFront ? 5 : 100);
      this._mcHighlight._visible = false;
   }
   function setLabel(sLabel)
   {
      var _loc3_;
      var _loc4_;
      if(this._bShowLabel)
      {
         this._lblText.text = sLabel;
         _loc3_ = Math.min(this._lblText.textWidth + 2,this.__width - 4);
         _loc4_ = this._lblText.textHeight;
         this._mcLabelBackground.clear();
         if(_loc3_ > 2 && _loc4_ != 0)
         {
            this.drawRoundRect(this._mcLabelBackground,2,2,_loc3_,_loc4_ + 2,0,0,50);
         }
      }
      else
      {
         this._lblText.removeMovieClip();
         this._mcLabelBackground.clear();
      }
   }
}
