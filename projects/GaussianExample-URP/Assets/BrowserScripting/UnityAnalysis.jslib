mergeInto(LibraryManager.library, {

  EngineLoaded: function()
  {
    if(typeof registerEngineLoaded === 'function')
    {
      registerEngineLoaded();
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
        let dataName = UTF8ToString(dataNameStr);
        let timeStampBigInt = BigInt(timeStampLow) | (BigInt(timestampHigh) << BigInt(32));
        registerStartLoadEvent(dataName, timeStampBigInt);
      }
  },

  FinishedLoadEvent: function (dataNameStr, timeStampLow, timestampHigh, durationMS)
  {
    if(typeof registerEndLoadEvent === 'function')
    {
      let dataName = UTF8ToString(dataNameStr);
      let timeStampBigInt = BigInt(timeStampLow) | (BigInt(timestampHigh) << BigInt(32));
      registerEndLoadEvent(dataName,timeStampBigInt, durationMS);
    }
  },

  BenchmarkComplete: function(){
    if(typeof benchmarkCompleted === 'function')
    {
      benchmarkCompleted();
    }
  }
});