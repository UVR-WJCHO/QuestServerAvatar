from fastapi import FastAPI, Query, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from fastapi.staticfiles import StaticFiles
from pydantic import BaseModel
import time
from typing import Dict, Optional

app = FastAPI()
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_methods=["*"],
    allow_headers=["*"],
)

# screenId -> { videoKey, t, updated_at }
latest: Dict[str, dict] = {}

class TimePayload(BaseModel):
    screenId: str          # "A" ...
    videoKey: str          # "intro", "clip7" ...
    t: float               # seconds

@app.post("/video/time")
def post_video_time(payload: TimePayload):
    sid = payload.screenId.strip().upper()
    vkey = payload.videoKey.strip()
    if not sid:
        raise HTTPException(status_code=400, detail="screenId is empty")
    if not vkey:
        raise HTTPException(status_code=400, detail="videoKey is empty")

    latest[sid] = {
        "screenId": sid,
        "videoKey": vkey,
        "t": float(payload.t),
        "updated_at": time.time(),
        "hasData": True,
    }
    return {"ok": True}

@app.get("/video/time")
def get_video_time(screenId: str = Query(...), videoKey: Optional[str] = None):
    sid = screenId.strip().upper()
    if sid not in latest:
        return {"screenId": sid, "videoKey": "", "t": 0.0, "updated_at": 0.0, "hasData": False}

    data = latest[sid].copy()

    # (선택) 유니티가 videoKey도 함께 보내면, "현재 그 영상이 맞는지" 확인 가능
    if videoKey is not None and videoKey.strip() != "" and data.get("videoKey") != videoKey.strip():
        data["videoKeyMismatch"] = True
    else:
        data["videoKeyMismatch"] = False

    return data

# 정적 웹 호스팅(맨 마지막)
app.mount("/", StaticFiles(directory="web", html=True), name="web")
