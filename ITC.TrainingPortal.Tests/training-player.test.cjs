const {test} = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const path = require('node:path');
const source = fs.readFileSync(path.join(__dirname, '../ITC.TrainingPortal.Web/wwwroot/js/training-player.js'), 'utf8');
function harness(rejectFirst = false) {
    const events = {}, requests = [];
    let unloaded = false;
    const classes = {remove(){}, toggle(){}};
    const player = {dataset:{resume:'29',videoId:'8',heartbeat:'/heartbeat'},currentTime:45,paused:false,seeking:false,
        addEventListener(name, callback){events[name] = callback;},
        pause(){this.paused = true;events.pause();},
        removeAttribute(){unloaded = true;},querySelectorAll(){return [];},load(){unloaded = true;}};
    const nodes = {trainingPlayer:player,videoStatus:{textContent:'In progress',classList:classes},
        videoProgress:{style:{},setAttribute(){}},playbackError:{classList:classes},videoPercentage:{}};
    vm.runInNewContext(source, {document:{getElementById:id=>nodes[id],querySelector:()=>({value:'token'})},
        window:{addEventListener(){}},setInterval(){},queueMicrotask,
        fetch:async (_,options)=>{
            requests.push(JSON.parse(options.body));
            if(rejectFirst && requests.length === 1) return {ok:false,status:422,json:async()=>({code:'playback_resync',acceptedPositionSeconds:29,message:'Press Play to continue.'})};
            return {ok:true,json:async()=>({acceptedPositionSeconds:29,isCompleted:false,progressPercentage:48})};
        }});
    return {player,events,requests,nodes,get unloaded(){return unloaded;}};
}
test('timing rejection rewinds to saved progress and retains the video', async()=>{
    const h = harness(true);
    await h.events.play();
    await new Promise(setImmediate);
    assert.equal(h.player.currentTime,29);
    assert.equal(h.player.paused,true);
    assert.equal(h.unloaded,false);
    assert.equal(h.requests[1].positionSeconds,29);
    assert.equal(h.requests[1].playbackState,'paused');
    assert.match(h.nodes.playbackError.textContent,/Press Play/);
});
test('transient seek position is never submitted', async()=>{
    const h = harness();h.player.seeking=true;
    await h.events.pause();assert.equal(h.requests.length,0);
    h.player.currentTime=29;h.player.seeking=false;
    await h.events.seeked();assert.equal(h.requests[0].positionSeconds,29);
});
