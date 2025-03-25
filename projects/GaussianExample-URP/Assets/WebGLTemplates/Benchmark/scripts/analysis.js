var loadTimes = [];
var frameTimes = [];
var avgFpsTimes= [];
var memorySnapshots = [];
var deviceInfo = {};

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
    
    // Combine all captured data into a single object
    const benchmarkData = {
        loadTimes: loadTimes,
        frameTimes: frameTimes,
        avgFpsTimes: avgFpsTimes,
        memorySnapshots: memorySnapshots,
        deviceInfo: deviceInfo
    };

    // Convert the object to a JSON string
    const jsonData = JSON.stringify(benchmarkData,  (_, v) =>
        typeof v === "bigint" ? v.toString() : v, 2);
    
    // Create a Blob from the JSON string
    const blob = new Blob([jsonData], { type: 'application/json' });

    // Create a link element
    const link = document.createElement('a');

    // Set the download attribute with a filename
    link.download = 'benchmark.json';

    // Create a URL for the Blob and set it as the href attribute
    link.href = URL.createObjectURL(blob);

    // Append the link to the document
    document.body.appendChild(link);

    // Programmatically click the link to trigger the download
    link.click();

    // Remove the link from the document
    document.body.removeChild(link);

    // Alert the user that the benchmark is complete
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
    let memoryUsage = takeMemorySnapshot(timeStamp, timeSinceLoad/1000.0);

    addLoadingRow("Finished", timeStamp, dataName,memoryUsage, durationMS);
}
function registerStartLoadEvent(dataName, timeStamp)
{
    logEvent(timeStamp, `Started loading ${dataName}`);

    let timeSinceLoad = calcTimeSinceLoad(timeStamp);
    let memoryUsage = takeMemorySnapshot(timeStamp, timeSinceLoad/1000.0);

    addLoadingRow("Started", timeStamp, dataName, memoryUsage);
}
function addLoadingRow(event, timeStamp, dataName, memoryUsage, durationMS ){

    const logTable = document.getElementById('logTable').getElementsByTagName('tbody')[0];
    const newRow = logTable.insertRow();
    const timestampCell = newRow.insertCell(0);
    const eventCell = newRow.insertCell(1);
    const dataNameCell = newRow.insertCell(2);
    const durationCell = newRow.insertCell(3);
    const memoryUsageCell = newRow.insertCell(4);
    
    eventCell.textContent = event;
    timestampCell.textContent = convertTimeStamp(timeStamp);
    dataNameCell.textContent = dataName;
    memoryUsageCell.textContent = memoryUsage !== undefined ? memoryUsage : '';

    if(durationMS !== undefined)
    {
        durationCell.textContent = durationMS;
    }
    else
    {
        durationCell.textContent = '';
    }
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
    return megaBytesUsed;
}

//retrieve information about the GPU
function getDeviceInfo()
{
    const deviceInfo = {};

    // Get the user agent string
    deviceInfo.userAgent = navigator.userAgent;

    // Get the platform
    deviceInfo.platform = navigator.userAgentData.platform;

    // Get the screen resolution
    deviceInfo.screenResolution = {
        width: window.screen.width,
        height: window.screen.height
    };

    // Get GPU information
    const canvas = document.createElement('canvas');
    const gl = canvas.getContext('webgl') || canvas.getContext('experimental-webgl');
    if (gl) {
        const debugInfo = gl.getExtension('WEBGL_debug_renderer_info');
        if (debugInfo) {
            deviceInfo.gpuVendor = gl.getParameter(debugInfo.UNMASKED_VENDOR_WEBGL);
            deviceInfo.gpuRenderer = gl.getParameter(debugInfo.UNMASKED_RENDERER_WEBGL);
        } else {
            deviceInfo.gpuVendor = 'Unknown';
            deviceInfo.gpuRenderer = 'Unknown';
        }
    } else {
        deviceInfo.gpuVendor = 'WebGL not supported';
        deviceInfo.gpuRenderer = 'WebGL not supported';
    }

    //TODO: Get unity graphics info: SystemInfo GraphicsDeviceID, GraphicsDeviceVendorID

    return deviceInfo;
}

function displayDeviceInfo() {
    const info = getDeviceInfo();
    const tableBody = document.getElementById('deviceInfoTable').getElementsByTagName('tbody')[0];
    deviceInfo = info;
    for (const key in info) {
        if (info.hasOwnProperty(key)) {
            const row = document.createElement('tr');
            const cellKey = document.createElement('td');
            const cellValue = document.createElement('td');

            cellKey.textContent = key;
            cellValue.textContent = typeof info[key] === 'object' ? JSON.stringify(info[key]) : info[key];

            row.appendChild(cellKey);
            row.appendChild(cellValue);
            tableBody.appendChild(row);
        }
    }
}