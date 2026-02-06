// manages loading and changing views
// partly recycled from another project of mine
// very wip

//variables
transitionLength = 300;

// functions

function preloadImage(url) {
  var img=new Image();
  img.src=url;
}

function changeView(v, t) {
  console.log('changeView called:', {view: v, transition: t});
  
  // transition in
  if (t != "none") {
    if (t == "fade") {
      console.log('Starting fade transition');
      $( ".black" ).addClass( "animate" );
      $( ".black" ).css( {"top" : "0"} );
      setTimeout(function(){
        console.log('Fade transition complete, loading view contents');
        loadViewContents(v, t);
      }, transitionLength);
    }
  } else {
    console.log('No transition, loading view directly');
    loadViewContents(v, t);
  }
}

function loadViewContents(v, t) {
  // Normal view loading
  if (v === "menu") {
    loadMenuView(v, t);
  } else {
    loadNormalView(v, t);
  }
}

function loadMenuView(v, t) {
  console.log('loadMenuView called:', {view: v, transition: t});
  
  // Show loading indicator
  $(".app").html('<div style="display: flex; justify-content: center; align-items: center; height: 100vh; color: white; font-family: Asap, sans-serif; font-size: 2em;">Loading...</div>');
  
  console.log('Loading menu view from:', "/customerui/views/" + v);
  console.log('.app element exists:', $('.app').length);
  console.log('.app is visible:', $('.app').is(':visible'));
  
  // Use $.ajax instead of .load() for better error handling
  $.ajax({
    url: "/customerui/views/" + v,
    method: 'GET',
    timeout: 10000,
    success: function(data) {
      console.log('Menu view loaded successfully, data length:', data.length);
      $('.app').html(data);
      
      setTimeout(function(){
        console.log('Waiting for images to load...');
        $('.app').imagesLoaded( { background: '*' }, function() {
          console.log('Images loaded, initializing menu...');
        // append date
        $(document).find( ".date" ).html( "<span> " + date + "</span>" );
        
        // CRITICAL: Clear any channel splash state when returning to menu from any view
        const mainMenu = document.querySelector('.main-menu');
        const body = document.body;
        const splashScreen = document.querySelector('.splash-screen');
        
        if (mainMenu) {
          mainMenu.classList.remove('channel-splash');
          console.log('Cleared channel-splash class from main menu on view load');
        }
        
        if (body) {
          body.classList.remove('channel-splash', 'splash-switch');
          console.log('Cleared channel splash classes from body on view load');
        }
        
        if (splashScreen) {
          splashScreen.innerHTML = '';
          splashScreen.style.removeProperty('background');
          splashScreen.style.removeProperty('background-image');
          splashScreen.style.removeProperty('background-color');
          console.log('Cleared splash screen content and styles on view load');
        }
        
        // Reset channel manager state when returning to menu
        if (typeof channelManager !== 'undefined' && channelManager) {
          console.log('Menu loaded - resetting channel manager state and refreshing channels');
          
          // Reset any active channel states
          channelManager.currentChannel = null;
          channelManager.isLoading = false;
          channelManager.isActivating = false;
          
          // Force refresh channels to prevent blank channels from cache
          channelManager.refreshChannels().catch(error => {
            console.error('Failed to refresh channels on menu load:', error);
            // Fallback to re-initializing channel manager
            channelManager.renderFallbackChannels();
          });
        } else {
          console.warn('Channel manager not available on menu load - will initialize when ready');
          // Set a flag to refresh channels when manager becomes available
          window.needsChannelRefresh = true;
        }

        currentView = v;

        // transition out
        if (t != "none") {
          if (t == "fade") {
            $( ".black" ).removeClass( "animate" );
            // play menu music (only if boot sequence hasn't already started it)
            if (typeof bootSequenceShown === 'undefined' || !bootSequenceShown) {
              var music = document.getElementById("bg-music");
              if (music) {
                music.currentTime = 0; // Reset to beginning for consistent experience
                music.play();
              }

              // play startup sound if it's the first time
              if (previousView === "default") {
                var startup = document.getElementById("startup");
                if (startup) {
                  startup.play();
                }
              }
            }
            setTimeout(function(){
              $( ".black" ).css( {"top" : "100vh"} );
            }, transitionLength);
          }
        }

        console.log("Menu view loaded: " + v);
      });
    }, 50); // Reduced timeout for faster loading
    },
    error: function(xhr, status, error) {
      console.error('AJAX Error loading menu view:', {xhr: xhr, status: status, error: error});
      $(".app").html('<div style="display: flex; justify-content: center; align-items: center; height: 100vh; color: red; font-family: Asap, sans-serif; font-size: 1.5em;">Error loading view: ' + status + ' - ' + error + '</div>');
    }
  });
}

function loadNormalView(v, t) {
  // Show loading indicator
  $(".app").html('<div style="display: flex; justify-content: center; align-items: center; height: 100vh; color: white; font-family: Asap, sans-serif; font-size: 2em;">Loading...</div>');
  
  $(".app").load("/customerui/views/" + v, function(response, status, xhr) {
    if (status == "error") {
      $(".app").html('<div style="display: flex; justify-content: center; align-items: center; height: 100vh; color: red; font-family: Asap, sans-serif; font-size: 1.5em;">Error loading view: ' + xhr.status + ' ' + xhr.statusText + '</div>');
      return;
    }
    
    setTimeout(function(){
      $('.app').imagesLoaded( { background: '*' }, function() {
        if (v === "settings-main") {
          // Stop background music when entering settings
          var music = document.getElementById("bg-music");
          if (music) {
            music.pause();
          }
          
          setTimeout(function(){
            $( ".settings-navcontainer" ).addClass( "animate" );
            $( ".settings-header" ).addClass( "animate" );
            $( ".settings-footer" ).addClass( "animate" );
          }, 300);
        }

        if (v === "licenses-temp") {
          // Stop background music when entering licenses (also a settings view)
          var music = document.getElementById("bg-music");
          if (music) {
            music.pause();
          }
          
          setTimeout(function(){
            $( ".settings-header" ).addClass( "animate" );
            $( ".settings-footer" ).addClass( "animate" );
            $( ".licenses-container" ).addClass( "animate" );
          }, 300);
        }

        if (v === "settings-wii") {
          // Stop background music when entering wii settings
          var music = document.getElementById("bg-music");
          if (music) {
            music.pause();
          }
          
          setTimeout(function(){
            $( ".settings-header" ).addClass( "animate" );
            $( ".settings-footer" ).addClass( "animate" );
            $( ".wii-settings-container" ).addClass( "animate" );
          }, 300);
        }

        if (v === "message-board") {
          // Stop background music when entering message board
          var music = document.getElementById("bg-music");
          if (music) {
            music.pause();
          }
        }

        currentView = v;

        // transition out
        if (t != "none") {
          if (t == "fade") {
            $( ".black" ).removeClass( "animate" );
            setTimeout(function(){
              $( ".black" ).css( {"top" : "100vh"} );
            }, transitionLength);
          }
        }

        console.log("View loaded: " + v);
      });
    }, 50); // Reduced timeout for faster loading
  });
}

// run after document load

$( document ).ready(function() {

  currentView = "default";
  previousView = "default";
  $(".black").show();

  // view change on click
  $("body").on("click", ".viewchange", function() {
	   viewtoChange = $(this).data("view");
     transition = $(this).data("transition");
     previousView = currentView;

     changeView(viewtoChange, transition);
	});

  // Check if we should show boot sequence first
  if (typeof shouldShowBootSequence === 'function' && shouldShowBootSequence()) {
    console.log('Starting with boot sequence');
    // Start boot sequence and wait for it to complete before loading menu
    if (typeof startWiiBootSequence === 'function') {
      startWiiBootSequence();
      
      // Set up a callback to load menu after boot sequence
      window.onBootSequenceComplete = function() {
        console.log('Boot sequence complete, loading menu');
        changeView("menu", "fade");
      };
    } else {
      // Fallback if boot sequence functions not available
      changeView("menu", "fade");
    }
  } else {
    // startup without boot sequence
    changeView("menu", "fade");
  }

});
