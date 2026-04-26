class dofus.graphics.gapi.controls.spellfullinfosvieweritem.SpellFullInfosViewerItem extends ank.gapi.core.UIBasicComponent
{
   var _ctrConditionalOver;
   var _ctrElement;
   var _ctrSpellArea;
   var _lbl;
   var _lblArea;
   var _nOverTextLinesCount;
   var _oItem;
   var _sOverText;
   var addToQueue;
   var arrange;
   var _nLabelWidth = 262.95;
   function SpellFullInfosViewerItem()
   {
      super();
      this._ctrSpellArea._visible = false;
      this._ctrConditionalOver._visible = false;
   }
   function setValue(bUsed, sSuggested, oItem)
   {
      var _loc6_ = _global.API;
      var _loc7_;
      var _loc8_;
      var _loc9_;
      var _loc10_;
      var _loc11_;
      var _loc12_;
      var _loc13_;
      var _loc14_;
      if(bUsed)
      {
         this._oItem = oItem;
         if(oItem.fx.description == undefined && oItem.description == undefined)
         {
            this._lbl.text = sSuggested;
         }
         else
         {
            if(oItem.fx.description != undefined)
            {
               this._lbl.text = oItem.fx.description;
            }
            else if(oItem.description != undefined)
            {
               this._lbl.text = oItem.description;
            }
            if(oItem.elementOverride != undefined)
            {
               _loc7_ = oItem.elementOverride;
               if(_loc7_ == "B")
               {
                  _loc13_ = this._lbl.text;
                  _loc14_ = _loc13_;
                  _loc14_ = _loc14_.split("(eau)").join("(meilleur élément)");
                  _loc14_ = _loc14_.split("(feu)").join("(meilleur élément)");
                  _loc14_ = _loc14_.split("(terre)").join("(meilleur élément)");
                  _loc14_ = _loc14_.split("(air)").join("(meilleur élément)");
                  _loc14_ = _loc14_.split("(neutre)").join("(meilleur élément)");
                  _loc14_ = _loc14_.split("(Eau)").join("(meilleur élément)");
                  _loc14_ = _loc14_.split("(Feu)").join("(meilleur élément)");
                  _loc14_ = _loc14_.split("(Terre)").join("(meilleur élément)");
                  _loc14_ = _loc14_.split("(Air)").join("(meilleur élément)");
                  _loc14_ = _loc14_.split("(Neutre)").join("(meilleur élément)");
                  this._lbl.text = _loc14_;
               }
            }
            else if(oItem.fx.element != undefined)
            {
               _loc7_ = oItem.fx.element;
            }
            else if(oItem.element != undefined)
            {
               _loc7_ = oItem.element;
            }
            if(_loc7_ != undefined)
            {
               switch(_loc7_)
               {
                  case "B":
                     this._ctrElement.contentPath = "";
                     break;
                  case "N":
                     this._ctrElement.contentPath = "IconNeutralDommage";
                     break;
                  case "F":
                     this._ctrElement.contentPath = "IconFireDommage";
                     break;
                  case "A":
                     this._ctrElement.contentPath = "IconAirDommage";
                     break;
                  case "W":
                     this._ctrElement.contentPath = "IconWaterDommage";
                     break;
                  case "E":
                     this._ctrElement.contentPath = "IconEarthDommage";
                     break;
                  default:
                     this._ctrElement.contentPath = "";
               }
            }
            else if(oItem.fx.icon != undefined)
            {
               this._ctrElement.contentPath = oItem.fx.icon;
            }
            else if(oItem.icon != undefined)
            {
               this._ctrElement.contentPath = oItem.icon;
            }
            else
            {
               this._ctrElement.contentPath = "";
            }
         }
         this._ctrConditionalOver.addEventListener("over",this);
         this._ctrConditionalOver.addEventListener("out",this);
         if(oItem.ar > 0)
         {
            _loc8_ = oItem.at.charCodeAt(0);
            this._ctrSpellArea.contentPath = dofus.Constants.EMBLEMS_SPELL_AREAS_PATH + _loc8_ + ".swf";
            this._ctrSpellArea._visible = true;
            this._ctrSpellArea.addEventListener("over",this);
            this._ctrSpellArea.addEventListener("out",this);
            this._lblArea.text = oItem.ar != 63 ? oItem.ar : _global.API.lang.getText("INFINIT_SHORT");
         }
         else
         {
            this._ctrSpellArea._visible = false;
            this._ctrSpellArea.removeEventListener("over",this);
            this._ctrSpellArea.removeEventListener("out",this);
            this._lblArea.text = "";
         }
         if(oItem.fx.conditions != undefined)
         {
            _loc9_ = oItem.fx.conditions;
         }
         else if(oItem.conditions != undefined)
         {
            _loc9_ = oItem.conditions;
         }
         if(_loc9_ == undefined || _loc9_[0] == _loc6_.lang.getText("NO_CONDITIONS"))
         {
            this._ctrConditionalOver._visible = false;
            this._sOverText = undefined;
         }
         else
         {
            this._nOverTextLinesCount = _loc9_.length;
            this._sOverText = _loc9_.join("\n- ");
            _loc10_ = "QuestionMark";
            if(_loc9_.length == 1)
            {
               _loc11_ = oItem.fx.conditionalStateID;
               if(_loc11_ == undefined)
               {
                  _loc11_ = oItem.conditionalStateID;
               }
               if(_loc11_ != undefined)
               {
                  _loc10_ = dofus.Constants.STATESICON_FILE;
               }
               else
               {
                  _loc12_ = oItem.fx.conditionalAlignmentID;
                  if(_loc12_ == undefined)
                  {
                     _loc12_ = oItem.conditionalAlignmentID;
                  }
                  if(_loc12_ != undefined)
                  {
                     _loc10_ = dofus.Constants.ALIGNMENTS_MINI_PATH + _loc12_ + ".swf";
                  }
               }
            }
            delete this._ctrConditionalOver.tempVars;
            if(_loc10_ == dofus.Constants.STATESICON_FILE)
            {
               this.setFightStateOnContainer(this._ctrConditionalOver,_loc11_);
            }
            this._ctrConditionalOver.addEventListener("onContentInitialized",this);
            this._ctrConditionalOver.contentPath = _loc10_;
            this._ctrConditionalOver._visible = true;
         }
         this.resizeLabel();
      }
      else if(this._lbl.text != undefined)
      {
         this._oItem = undefined;
         this._lbl.text = "";
         this._lblArea.text = "";
         this._ctrSpellArea._visible = false;
         this._ctrElement.contentPath = "";
         this._ctrConditionalOver._visible = false;
      }
      else
      {
         this._oItem = undefined;
      }
   }
   function init()
   {
      super.init(false);
   }
   function createChildren()
   {
      this.arrange();
   }
   function size()
   {
      super.size();
      this.addToQueue({object:this,method:this.arrange});
   }
   function resizeLabel()
   {
      this._lbl.width = this._nLabelWidth;
      if(!this._ctrSpellArea._visible)
      {
         this._lbl.width += 49;
      }
   }
   function onContentInitialized(oEvent)
   {
      var _loc3_ = oEvent.target;
      if(_loc3_.tempVars)
      {
         this.setFightStateOnContainer(_loc3_,_loc3_.tempVars.fightStateToPut);
      }
   }
   function setFightStateOnContainer(ctr, nState)
   {
      var _loc3_;
      var _loc4_;
      if(ctr.contentLoaded)
      {
         delete ctr.tempVars;
         _loc3_ = "State_" + nState;
         ctr.content._mcState.removeMovieClip();
         _loc4_ = ctr.content.attachMovie(_loc3_,"_mcState",ctr.content.getNextHighestDepth());
         ctr.sizeContent();
         _loc4_._xscale += 70;
         _loc4_._yscale += 70;
         _loc4_._x += 6;
         _loc4_._y += 6;
      }
      else
      {
         ctr.tempVars = {fightStateToPut:nState};
      }
   }
   function over(oEvent)
   {
      var _loc4_ = _global.API;
      var _loc5_;
      var _loc6_;
      var _loc7_;
      var _loc8_;
      switch(oEvent.target)
      {
         case this._ctrConditionalOver:
            if(this._sOverText != undefined)
            {
               _loc5_ = _loc4_.lang.getText("CONDITIONS") + "\n- " + this._sOverText;
               _loc6_ = -20 - this._nOverTextLinesCount * 12;
               _loc4_.ui.showTooltip(_loc5_,oEvent.target,_loc6_);
            }
            return;
         case this._ctrSpellArea:
            _loc7_ = this._oItem.at.charCodeAt(0);
            _loc8_ = _loc4_.lang.getText("EFFECT_SHAPE_TYPE_" + _loc7_,[this._oItem.ar != 63 ? this._oItem.ar : _loc4_.lang.getText("INFINIT")]);
            _loc4_.ui.showTooltip(_loc8_,oEvent.target,-20);
      }
      return undefined;
   }
   function out(oEvent)
   {
      _global.API.ui.hideTooltip();
   }
}
