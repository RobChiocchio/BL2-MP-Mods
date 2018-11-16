function updateLoadingBar(percent){
    var loadingBarProgress = document.getElementById("loadingBarProgress");
    loadingBarProgress.style.width = percent + '%';
};

function testLoadingBar(){
    var id = setInterval(frame, 10);
    var width = 0;
    function frame(){
        if (width >= 100) {
            clearInterval(id);
        } else {
            width++; 
            updateLoadingBar(width); 
        }
    }
};