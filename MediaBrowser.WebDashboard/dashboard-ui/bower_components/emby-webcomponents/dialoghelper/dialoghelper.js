define(["appRouter","focusManager","browser","layoutManager","inputManager","dom","css!./dialoghelper.css","scrollStyles"],function(appRouter,focusManager,browser,layoutManager,inputManager,dom){"use strict";function enableAnimation(){return!browser.tv&&browser.supportsCssAnimation()}function removeCenterFocus(dlg){layoutManager.tv&&(dlg.classList.contains("scrollX")?centerFocus(dlg,!0,!1):dlg.classList.contains("smoothScrollY")&&centerFocus(dlg,!1,!1))}function tryRemoveElement(elem){var parentNode=elem.parentNode;if(parentNode)try{parentNode.removeChild(elem)}catch(err){console.log("Error removing dialog element: "+err)}}function DialogHashHandler(dlg,hash,resolve){function onHashChange(e){var isBack=self.originalUrl===window.location.href;!isBack&&isOpened(dlg)||window.removeEventListener("popstate",onHashChange),isBack&&(self.closedByBack=!0,closeDialog(dlg))}function onBackCommand(e){"back"===e.detail.command&&(self.closedByBack=!0,e.preventDefault(),e.stopPropagation(),closeDialog(dlg))}function onDialogClosed(){if(isHistoryEnabled(dlg)||inputManager.off(dlg,onBackCommand),window.removeEventListener("popstate",onHashChange),removeBackdrop(dlg),dlg.classList.remove("opened"),removeScrollLockOnClose&&document.body.classList.remove("noScroll"),!self.closedByBack&&isHistoryEnabled(dlg)){(history.state||{}).dialogId===hash&&history.back()}if(layoutManager.tv&&focusManager.focus(activeElement),"false"!==dlg.getAttribute("data-removeonclose")){removeCenterFocus(dlg);var dialogContainer=dlg.dialogContainer;dialogContainer?(tryRemoveElement(dialogContainer),dlg.dialogContainer=null):tryRemoveElement(dlg)}setTimeout(function(){resolve({element:dlg,closedByBack:self.closedByBack})},1)}var self=this;self.originalUrl=window.location.href;var activeElement=document.activeElement,removeScrollLockOnClose=!1;dlg.addEventListener("close",onDialogClosed),!dlg.classList.contains("dialog-fixedSize")&&dlg.classList.add("centeredDialog"),dlg.classList.remove("hide"),addBackdropOverlay(dlg),dlg.classList.add("opened"),dlg.dispatchEvent(new CustomEvent("open",{bubbles:!1,cancelable:!1})),"true"!==dlg.getAttribute("data-lockscroll")||document.body.classList.contains("noScroll")||(document.body.classList.add("noScroll"),removeScrollLockOnClose=!0),animateDialogOpen(dlg),isHistoryEnabled(dlg)?(appRouter.pushState({dialogId:hash},"Dialog","#"+hash),window.addEventListener("popstate",onHashChange)):inputManager.on(dlg,onBackCommand)}function addBackdropOverlay(dlg){var backdrop=document.createElement("div");backdrop.classList.add("dialogBackdrop");var backdropParent=dlg.dialogContainer||dlg;backdropParent.parentNode.insertBefore(backdrop,backdropParent),dlg.backdrop=backdrop,backdrop.offsetWidth,backdrop.classList.add("dialogBackdropOpened"),dom.addEventListener(dlg.dialogContainer||backdrop,"click",function(e){e.target===dlg.dialogContainer&&close(dlg)},{passive:!0})}function isHistoryEnabled(dlg){return"true"===dlg.getAttribute("data-history")}function open(dlg){globalOnOpenCallback&&globalOnOpenCallback(dlg);var parent=dlg.parentNode;parent&&parent.removeChild(dlg);var dialogContainer=document.createElement("div");return dialogContainer.classList.add("dialogContainer"),dialogContainer.appendChild(dlg),dlg.dialogContainer=dialogContainer,document.body.appendChild(dialogContainer),new Promise(function(resolve,reject){new DialogHashHandler(dlg,"dlg"+(new Date).getTime(),resolve)})}function isOpened(dlg){return!dlg.classList.contains("hide")}function close(dlg){isOpened(dlg)&&(isHistoryEnabled(dlg)?history.back():closeDialog(dlg))}function closeDialog(dlg){if(!dlg.classList.contains("hide")){dlg.dispatchEvent(new CustomEvent("closing",{bubbles:!1,cancelable:!1}));animateDialogClose(dlg,function(){focusManager.popScope(dlg),dlg.classList.add("hide"),dlg.dispatchEvent(new CustomEvent("close",{bubbles:!1,cancelable:!1}))})}}function animateDialogOpen(dlg){var onAnimationFinish=function(){focusManager.pushScope(dlg),"true"===dlg.getAttribute("data-autofocus")&&focusManager.autoFocus(dlg)};if(enableAnimation()){var onFinish=function(){dom.removeEventListener(dlg,dom.whichAnimationEvent(),onFinish,{once:!0}),onAnimationFinish()};return void dom.addEventListener(dlg,dom.whichAnimationEvent(),onFinish,{once:!0})}onAnimationFinish()}function animateDialogClose(dlg,onAnimationFinish){if(enableAnimation()){var animated=!0;switch(dlg.animationConfig.exit.name){case"fadeout":dlg.style.animation="fadeout "+dlg.animationConfig.exit.timing.duration+"ms ease-out normal both";break;case"scaledown":dlg.style.animation="scaledown "+dlg.animationConfig.exit.timing.duration+"ms ease-out normal both";break;case"slidedown":dlg.style.animation="slidedown "+dlg.animationConfig.exit.timing.duration+"ms ease-out normal both";break;default:animated=!1}var onFinish=function(){dom.removeEventListener(dlg,dom.whichAnimationEvent(),onFinish,{once:!0}),onAnimationFinish()};if(dom.addEventListener(dlg,dom.whichAnimationEvent(),onFinish,{once:!0}),animated)return}onAnimationFinish()}function shouldLockDocumentScroll(options){return!(supportsOverscrollBehavior&&(options.size||!browser.touch))&&(null!=options.lockScroll?options.lockScroll:"fullscreen"===options.size||(!!options.size||browser.touch))}function removeBackdrop(dlg){var backdrop=dlg.backdrop;if(backdrop){dlg.backdrop=null;var onAnimationFinish=function(){tryRemoveElement(backdrop)};if(enableAnimation())return backdrop.classList.remove("dialogBackdropOpened"),void setTimeout(onAnimationFinish,300);onAnimationFinish()}}function centerFocus(elem,horiz,on){require(["scrollHelper"],function(scrollHelper){var fn=on?"on":"off";scrollHelper.centerFocus[fn](elem,horiz)})}function createDialog(options){options=options||{};var dlg=document.createElement("div");dlg.classList.add("focuscontainer"),dlg.classList.add("hide"),shouldLockDocumentScroll(options)&&dlg.setAttribute("data-lockscroll","true"),!1!==options.enableHistory&&appRouter.enableNativeHistory()&&dlg.setAttribute("data-history","true"),!1!==options.modal&&dlg.setAttribute("modal","modal"),!1!==options.autoFocus&&dlg.setAttribute("data-autofocus","true");var defaultEntryAnimation,defaultExitAnimation;defaultEntryAnimation="scaleup",defaultExitAnimation="scaledown";var entryAnimation=options.entryAnimation||defaultEntryAnimation,exitAnimation=options.exitAnimation||defaultExitAnimation,entryAnimationDuration=options.entryAnimationDuration||("fullscreen"!==options.size?180:280),exitAnimationDuration=options.exitAnimationDuration||("fullscreen"!==options.size?120:220);if(dlg.animationConfig={entry:{name:entryAnimation,timing:{duration:entryAnimationDuration,easing:"ease-out"}},exit:{name:exitAnimation,timing:{duration:exitAnimationDuration,easing:"ease-out",fill:"both"}}},dlg.classList.add("dialog"),options.scrollX?(dlg.classList.add("scrollX"),dlg.classList.add("smoothScrollX"),layoutManager.tv&&centerFocus(dlg,!0,!0)):!1!==options.scrollY&&(dlg.classList.add("smoothScrollY"),layoutManager.tv&&centerFocus(dlg,!1,!0)),options.removeOnClose&&dlg.setAttribute("data-removeonclose","true"),options.size&&(dlg.classList.add("dialog-fixedSize"),dlg.classList.add("dialog-"+options.size)),enableAnimation())switch(dlg.animationConfig.entry.name){case"fadein":dlg.style.animation="fadein "+entryAnimationDuration+"ms ease-out normal";break;case"scaleup":dlg.style.animation="scaleup "+entryAnimationDuration+"ms ease-out normal both";break;case"slideup":dlg.style.animation="slideup "+entryAnimationDuration+"ms ease-out normal";break;case"slidedown":dlg.style.animation="slidedown "+entryAnimationDuration+"ms ease-out normal"}return dlg}var globalOnOpenCallback,supportsOverscrollBehavior="overscroll-behavior-y"in document.body.style;return{open:open,close:close,createDialog:createDialog,setOnOpen:function(val){globalOnOpenCallback=val}}});