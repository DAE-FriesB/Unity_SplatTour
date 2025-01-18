var loadTimes = [];
var frameTimes = [];
var avgFpsTimes= [];
var memorySnapshots = [];

var currentFrameIdx = 0;

var currentFps = 0;
var memoryChart = null;
var combinedChart = null;
var startLoadTimeStamp = 0;
var runningTime = 0;
var maxFrameCount = 0;
const FRAME_STEP = 50;

function convertTimeStamp(unixTimeStampMS){
    let date = new Date(Number(unixTimeStampMS));
    //date = new Date(date.getTime() - (date.getTimezoneOffset() * 60000));
    return date.toLocaleTimeString('en-US', { hour12: false }) + '.' + String(date.getMilliseconds()).padStart(3, '0');
}
function calcTimeSinceLoad(unixTimeStampMS){
    if(typeof unixTimeStampMS === 'bigint'){
        return Number(unixTimeStampMS - BigInt(startLoadTimeStamp));
    }
    return unixTimeStampMS - startLoadTimeStamp;
}

//gets called right before the application is loaded.
function startEngineLoad(){
    startLoadTimeStamp = Date.now();
    registerStartLoadEvent("Engine", startLoadTimeStamp);
}
//gets called from the application, right after the application has started.
function registerEngineLoaded(){
    endLoadTimeStamp = Date.now();
    loadTimes.push(endLoadTimeStamp - startLoadTimeStamp);
    registerEndLoadEvent("Engine", endLoadTimeStamp, endLoadTimeStamp - startLoadTimeStamp);
}

function benchmarkCompleted(){
    alert('Benchmark complete!');
}

function registerEndLoadEvent(dataName, timeStamp, durationMS)
{
    let data = {};
    data.name = dataName;
    data.timeStamp = timeStamp;
    data.durationMS = durationMS;
    loadTimes.push(data);

    logEvent(timeStamp, `Finished loading ${dataName} in ${durationMS}ms`);

    //memory snapshot
    let timeSinceLoad = calcTimeSinceLoad(timeStamp);
    takeMemorySnapshot(timeStamp, timeSinceLoad/1000.0);

    addLoadingRow("Finished Loading", timeStamp, dataName, durationMS);
}
function registerStartLoadEvent(dataName, timeStamp)
{
    logEvent(timeStamp, `Started loading ${dataName}`);
    addLoadingRow("Started Loading", timeStamp, dataName, 0);
}
function addLoadingRow(event, timeStamp, dataName, durationMS){

    const logTable = document.getElementById('logTable').getElementsByTagName('tbody')[0];
    const newRow = logTable.insertRow();
    const eventCell = newRow.insertCell(0);
    const timestampCell = newRow.insertCell(1);
    const dataNameCell = newRow.insertCell(2);
    const durationCell = newRow.insertCell(3);

    eventCell.textContent = event;
    timestampCell.textContent = convertTimeStamp(timeStamp);
    dataNameCell.textContent = dataName;
    durationCell.textContent = durationMS;
}



function logEvent(timestamp, eventMessage)
{
    console.log(`[${convertTimeStamp(timestamp)}] ${eventMessage}`);
}

function updateFPSCounter(averageFPS) {
    currentFps = averageFPS;
    const fpsCounter = document.getElementById('fpsCounter');
    if (fpsCounter) {
        fpsCounter.textContent = `${averageFPS.toFixed(2)} FPS`;
    }

}

function registerFrameTime(frameTimeMS)
{
    runningTime += frameTimeMS/1000;

    frameTimes.push(frameTimeMS);
    avgFpsTimes.push(currentFps);
    ++currentFrameIdx;

    if(currentFrameIdx % FRAME_STEP === 0){
        takeMemorySnapshot();
    }


    updateFPSCharts();
}

function updateFPSCharts(){

    if(currentFrameIdx >= maxFrameCount)
    {
        for(let idx = 0; idx < FRAME_STEP; idx++)
        {
            combinedChart.data.labels.push(maxFrameCount);
            maxFrameCount++
        }

        combinedChart.data.datasets[0].data = frameTimes;
        combinedChart.data.datasets[1].data = avgFpsTimes;
    }
    // fpsChart.update('none');
    // frameTimesChart.update('none');
    combinedChart.update('none');
}

var initFPSCharts = function(){
    initCombinedCharts();
    initMemoryChart();
    updateFPSCharts();
};

var initCombinedCharts= function(){
    const ctx = document.getElementById('combinedChart');
    combinedChart = new Chart(ctx, {
        type: 'bar', // Base type
        data: {
            labels: frameTimes.map((_, i) => i),
            datasets: [
                {
                    type: 'bar', // Bar chart for Frame Time (ms)
                    label: 'Frame Time (ms)',
                    data: frameTimes,
                    backgroundColor: 'rgb(244, 138, 0)',
                    yAxisID: 'y1',
                    order: 2
                },
                {
                    type: 'line', // Line chart for Frame Rate (FPS)
                    label: 'Frame Rate (FPS)',
                    data: avgFpsTimes,
                    borderColor: 'rgb(19, 68, 247)',
                    borderWidth: 1,
                    pointRadius: 0,
                    fill: false,
                    yAxisID: 'y2',
                    order: 1
                }
            ]
        },
        options: {
            maintainAspectRatio: false,
            scales: {
                x: {
                    title: {
                        display: true,
                        text: 'Frame Number'
                    },
                    beginAtZero: true
                },
                y1: {
                    type: 'linear',
                    position: 'left',
                    title: {
                        display: true,
                        text: 'Time (ms)'
                    },
                    beginAtZero: true,
                    max: 200,
                },
                y2: {
                    type: 'linear',
                    position: 'right',
                    title: {
                        display: true,
                        text: 'Frame Rate (FPS)'
                    },
                    beginAtZero: true,
                    suggestedMax: 60,
                    grid: {
                        drawOnChartArea: false // Only want the grid lines for one axis to show up
                    },
                }
            }
        }
    });

};

var initMemoryChart = function(){
    const ctx = document.getElementById('memoryChart');
    memoryChart = new Chart(ctx, {
        type: 'line',
        data: {
            labels: [],
            datasets: [
                {
                    label: 'Memory Usage (MB)',
                    data: [],
                    borderColor: 'rgb(70, 0, 84)',
                    borderWidth: 5,
                    pointRadius: 0,
                    fill: true
                }
            ]
        },
        options: {
            maintainAspectRatio: false,
            scales: {
                x: {
                    title: {
                        display: true,
                        text: 'Time'
                    },
                    beginAtZero: true
                    
                },
                y: {
                    type: 'linear',
                    position: 'left',
                    title: {
                        display: true,
                        text: 'Memory Usage (MB)'
                    },
                    beginAtZero: true
                }
            }
        }
    });
    takeMemorySnapshot(Date.now(),0);
};

var takeMemorySnapshot = function(timestamp, timeSinceLoad){

    if(timestamp === undefined)
    {
        timestamp = Date.now();
        timeSinceLoad = calcTimeSinceLoad(timestamp)/1000.0;
    }

    const megaBytesUsed = fetchMemoryInfo();
    memorySnapshots.push({timestamp: timestamp, memoryInfo: megaBytesUsed});
    if(megaBytesUsed > 0)
    {
        const memoryData = {
            x: timeSinceLoad,
            y: megaBytesUsed
        };
        if(memoryChart)
        {
            memoryChart.data.labels.push(convertTimeStamp(timestamp));
            memoryChart.data.datasets[0].data.push(memoryData);
            memoryChart.update('none');
        }
    }
}
