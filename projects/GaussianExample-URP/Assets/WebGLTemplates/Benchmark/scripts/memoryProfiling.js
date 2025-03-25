var fetchMemoryInfo = function() {
    // David Vallejo (@thyng)
    // Analytics Debugger S.L.U. 2023

    // This is only available on Chromium based browsers, just skip if the API is not available
    if (!(window.performance && 'memory'in window.performance))
        return 0;

    try{
    
    return performance.memory.usedJSHeapSize/1024/1024;

    
    }catch(e){}
};
