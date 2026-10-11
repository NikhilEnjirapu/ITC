(()=>{
const player=document.getElementById('trainingPlayer'), status=document.getElementById('videoStatus'), bar=document.getElementById('videoProgress'), error=document.getElementById('playbackError');
const token=document.querySelector('#heartbeatToken input').value;
let accepted=Number(player.dataset.resume)||0, furthest=accepted, adjusting=false, busy=false, pending=false, stopped=false, lastTime=accepted, completionHandled=status.textContent.includes('Completed');
function stop(message){if(stopped)return;stopped=true;player.pause();player.removeAttribute('src');player.querySelectorAll('source').forEach(x=>x.remove());player.load();error.textContent=message;error.classList.remove('d-none');}
player.addEventListener('loadedmetadata',()=>{adjusting=true;player.currentTime=Math.min(accepted,player.duration);lastTime=player.currentTime;adjusting=false;});
player.addEventListener('seeking',()=>{if(!adjusting&&player.currentTime>furthest+0.25){adjusting=true;player.currentTime=furthest;adjusting=false;}});
player.addEventListener('ratechange',()=>{if(player.playbackRate!==1){player.playbackRate=1;error.textContent='Playback speed must remain at normal speed.';error.classList.remove('d-none');}});
player.addEventListener('timeupdate',()=>{if(!player.seeking&&player.currentTime-lastTime<=1&&player.currentTime>=lastTime)furthest=Math.max(furthest,player.currentTime);lastTime=player.currentTime;});
async function heartbeat(){
 if(stopped)return;if(busy||player.seeking){pending=true;return;}busy=true;pending=false;
 try{
 const response=await fetch(player.dataset.heartbeat,{method:'POST',headers:{'Content-Type':'application/json','RequestVerificationToken':token},body:JSON.stringify({videoId:Number(player.dataset.videoId),positionSeconds:player.currentTime,playbackState:player.ended?'ended':player.paused?'paused':'playing'})});
 if(!response.ok){
  let message='Playback could not be validated. Reload to resume.', failure;
  try{failure=await response.json();message=failure.message||message;}catch{}
  if(response.status===422&&failure?.code==='playback_resync'&&Number.isFinite(failure.acceptedPositionSeconds)){
   player.pause();accepted=failure.acceptedPositionSeconds;furthest=accepted;lastTime=accepted;
   adjusting=true;player.currentTime=accepted;adjusting=false;
   error.textContent=message;error.classList.remove('d-none');pending=true;return;
  }
  stop(message);return;
 }
 const body=await response.json();accepted=body.acceptedPositionSeconds;
 const percentage=body.isCompleted?100:body.progressPercentage;
 bar.style.width=percentage+'%';bar.setAttribute('aria-valuenow',percentage);
 document.getElementById('videoPercentage').textContent=percentage+'%';
 status.textContent=body.isCompleted?'✓ Completed':'In progress';
 status.classList.toggle('text-success',body.isCompleted);status.classList.toggle('fw-semibold',body.isCompleted);
 if(body.isCompleted&&!completionHandled){
  completionHandled=true;
  const availability=document.getElementById('examAvailability');
  if(availability){
   try{
    const examResponse=await fetch(availability.dataset.availabilityUrl,{cache:'no-store'});
    if(examResponse.ok&&!examResponse.redirected){
     const examHtml=await examResponse.text();
     const examLink=new DOMParser().parseFromString(examHtml,'text/html').querySelector('#examAvailability.exam-available a');
     if(examLink){window.top.location.assign(examLink.href);return;}
    }
   }catch{/* Keep the completed state visible; the normal availability poll can retry. */}
  }
  const next=document.getElementById('nextVideo');if(next&&next.tagName==='BUTTON')location.reload();
 }
 }catch{stop('Connection lost. Reload to resume from your saved position.');}finally{busy=false;if(pending&&!stopped){pending=false;queueMicrotask(heartbeat);}}
}
async function check(){if(stopped)return;try{const r=await fetch(player.dataset.status,{cache:'no-store'});if(!r.ok||!(await r.json()).allowed)stop('Training is unavailable while an exam is in progress.');}catch{stop('Unable to verify access. Reload to resume.');}}
player.addEventListener('play',heartbeat);player.addEventListener('pause',heartbeat);player.addEventListener('ended',heartbeat);
player.addEventListener('seeked',heartbeat);
setInterval(heartbeat,2000);setInterval(check,2000);
window.addEventListener('storage',e=>{if(e.key==='training-exam-started')check();});
})();
