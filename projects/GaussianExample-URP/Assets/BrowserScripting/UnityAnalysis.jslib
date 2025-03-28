mergeInto(LibraryManager.library, {

  EngineLoaded: function(gpuNameStr, gpuId, gpuVendorStr, gpuVersionStr, gpuVendorId)
  {
    const gpuName = UTF8ToString(gpuNameStr);
    const gpuVersion = UTF8ToString(gpuVersionStr);
    const gpuVendor = UTF8ToString(gpuVendorStr);
    var gpuInfo = {
      name: gpuName,
      Id: gpuId,
      vendor: gpuVendor,
      vendorId: gpuVendorId,
      version: gpuVersion
    };
    if(typeof registerEngineLoaded === 'function')
    {
      registerEngineLoaded(gpuInfo);
      //registerEngineLoaded();
    }
  },

  FPSEvent: function (currentFrameTimeMS, averageFPS)
  {
    if (typeof updateFPSCounter === 'function') 
    {
      updateFPSCounter(averageFPS);
    }
    if(typeof registerFrameTime === 'function')
    {
      registerFrameTime(currentFrameTimeMS);
    }
  },

  StartLoadEvent: function (dataNameStr, timeStampLow, timestampHigh) 
  {
      if(typeof registerStartLoadEvent === 'function')
      {
        const dataName = UTF8ToString(dataNameStr);
        // Convert the original number to its binary representation

        registerStartLoadEvent(dataName, 0);
      }
  },

  FinishedLoadEvent: function (dataNameStr, timeStampLow, timestampHigh, durationMS)
  {
    if(typeof registerEndLoadEvent === 'function')
    {
      let dataName = UTF8ToString(dataNameStr);

      registerEndLoadEvent(dataName,0, durationMS);
    }
  },

  BenchmarkComplete: function(){
    if(typeof benchmarkCompleted === 'function')
    {
      benchmarkCompleted();
    }
  }
});