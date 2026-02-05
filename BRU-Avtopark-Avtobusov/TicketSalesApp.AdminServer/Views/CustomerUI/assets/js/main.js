// Wii Boot Sequence
var bootSequenceShown = false;

function startWiiBootSequence() {
  console.log('Starting Wii boot sequence');
  
  // Add booting class to body
  $('body').addClass('wii-booting');
  
  // Show boot sequence
  $('#wii-boot-sequence').removeClass('hidden');
  
  // Debug: Check if elements exist
  console.log('Boot sequence element:', $('#wii-boot-sequence').length);
  console.log('Logo screen element:', $('#wii-logo-screen').length);
  console.log('Logo image element:', $('.wii-logo-image').length);
  
  // Show Wii logo for 3 seconds
  setTimeout(function() {
    console.log('Starting transition from logo to health screen');
    
    // Start the transition: fade logo screen to black and make it disappear
    $('#wii-logo-screen').addClass('fade-to-black');
    
    // After the background turns black and logo fades, show health screen
    setTimeout(function() {
      $('#wii-health-safety-screen').addClass('show');
      
      // Add click/keypress handlers for health and safety screen
      $(document).on('click.wii-boot keypress.wii-boot', function(e) {
        if (e.type === 'click' || e.which) {
          finishWiiBootSequence();
        }
      });
      
    }, 1000); // Wait for the white-to-black transition to complete
  }, 3000); // Show logo for 3 seconds
}

function finishWiiBootSequence() {
  console.log('Finishing Wii boot sequence');
  
  // Remove event handlers
  $(document).off('click.wii-boot keypress.wii-boot');
  
  // Fade out health and safety screen
  $('#wii-health-safety-screen').addClass('fade-out');
  
  setTimeout(function() {
    console.log('Boot sequence fade out complete, hiding elements');
    
    // Hide boot sequence completely
    $('#wii-boot-sequence').addClass('hidden');
    $('#wii-boot-sequence').hide(); // Force hide with jQuery
    
    // Remove booting class from body
    $('body').removeClass('wii-booting');
    
    // Mark boot sequence as shown
    bootSequenceShown = true;
    
    console.log('Boot sequence hidden, body class removed');
    
    // Play startup sound if available
    var startupAudio = document.getElementById("startup");
    if (startupAudio) {
      startupAudio.play().catch(function(error) {
        console.log('Could not play startup sound:', error);
      });
    }
    
    // Start background music if available
    var bgMusic = document.getElementById("bg-music");
    if (bgMusic) {
      bgMusic.play().catch(function(error) {
        console.log('Could not play background music:', error);
      });
    }
    
    // Call the completion callback if it exists
    if (typeof window.onBootSequenceComplete === 'function') {
      console.log('Calling onBootSequenceComplete callback');
      window.onBootSequenceComplete();
    } else {
      console.error('onBootSequenceComplete callback not found!');
    }
    
  }, 1000); // Wait for fade out to complete (increased from 500ms to match CSS transition)
}

function shouldShowBootSequence() {
  // Show boot sequence if:
  // 1. It hasn't been shown yet in this session
  // 2. Or if we're loading the menu view (even from cache)
  return !bootSequenceShown;
}

// Function to manually trigger boot sequence (useful for testing or re-showing)
function forceBootSequence() {
  bootSequenceShown = false;
  startWiiBootSequence();
}

//delay
var delay = ( function() {
    var timer = 0;
    return function(callback, ms) {
        clearTimeout (timer);
        timer = setTimeout(callback, ms);
    };
})();

// UI audio
function hover(){
	var audio = document.getElementById("hover");
	audio.play();
}

// click audio
function select(){
	var audio = document.getElementById("select");
	audio.play();
}

// zip audio
function zip(){
	var audio = document.getElementById("zip");
	audio.play();
	select();
	var music = document.getElementById("bg-music");
	music.pause();
}

// back
function back(){
	var audio = document.getElementById("back");
	audio.play();
}

// date
const monthNames = ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"];
const d = new Date();

weekday = monthNames[d.getDay()];
day = d.getDate();
month = d.getMonth() + 1;
date = weekday + " " + month + "/" + day;

$( document ).ready(function() {
  // Wait for channel manager to be available before setting up fallback
  function waitForChannelManager() {
    if (typeof channelManager !== 'undefined' && channelManager) {
      console.log('Channel manager is available, disabling fallback handlers');
      
      // Remove any existing fallback handlers to prevent conflicts
      $("body").off("click", ".occupied .hover");
      
      console.log('Fallback handlers disabled - channel manager will handle all activations');
      return;
    }
    
    console.warn('Channel manager not loaded, using fallback channel handling');
    
    // Fallback: go to main menu when channel is clicked (only if channel manager not available)
    $("body").off("click", ".occupied .hover"); // Remove existing handlers first
    $("body").on("click", ".occupied .hover", function(e){
      // Double-check that channel manager is still not available
      if (typeof channelManager !== 'undefined' && channelManager) {
        console.log('Channel manager became available, ignoring fallback handler');
        return;
      }
      
      console.log('Fallback channel activation triggered');
      var centerX = $(this).offset().left + $(this).width() / 2;
      var centerY = $(this).offset().top + $(this).height() / 2;
      $( ".main-menu" ).css( {"transform-origin" : centerX + "px " + centerY + "px 0px"} );

      var img = $( this ).attr( "data-img" );
      $( ".splash-screen" ).css( {"background-image" : " url(" + img + ")", "transform-origin" : centerX + "px " + centerY + "px 0px"} );

      $( ".main-menu" ).addClass( "channel-splash" );
      $( "body" ).addClass( "channel-splash" );
      delay(function(){
        $( "body" ).removeClass( "splash-switch" );
      }, 900 );
    });
  }
  
  // Check immediately and also after delays
  waitForChannelManager();
  setTimeout(waitForChannelManager, 1000);
  setTimeout(waitForChannelManager, 3000);
  setTimeout(waitForChannelManager, 5000); // Additional check
   

  // back to main menu
  $("body").on("click", ".menu-btn", function(){
    if (typeof channelManager !== 'undefined' && channelManager.returnToMenu) {
      channelManager.returnToMenu();
    } else {
      // Fallback
      $( ".main-menu" ).removeClass( "channel-splash" );
      $( "body" ).removeClass( "channel-splash" );
      $( "body" ).addClass( "splash-switch" );
      delay(function(){
        $( "body" ).removeClass( "splash-switch" );
      }, 900 );
    }
  });

  // ignore screen warning
  $("body").on("click", ".screen-message", function(){
    $( ".screen-message" ).addClass( "hidden" );
  });
});
