"""Online Mandarin speech worker. Dialogue is data; original wording is retained."""
import asyncio
import json
import os
from pathlib import Path
import sys
import edge_tts

def settings(text, role, config):
    voice = dict(config['roles'].get(role, config['roles']['default']))
    voice['delivery'] = 'steady'
    for item in config['delivery']:
        if any(word in text for word in item['contains']):
            voice['rate'] += item['rate']
            voice['pitch'] += item['pitch']
            voice['delivery'] = item['name']
            break
    return voice

async def synthesize(text, role, destination, config):
    choice = settings(text, role, config)
    async def request():
        for attempt in range(3):
            speech = edge_tts.Communicate(text, choice['voice'], rate=f"{choice['rate']:+d}%", pitch=f"{choice['pitch']:+d}Hz", connect_timeout=6, receive_timeout=12)
            try:
                await speech.save(str(destination))
                return
            except Exception:
                if attempt == 2:
                    raise
                await asyncio.sleep(.6 * (attempt + 1))
    # Retry transient empty responses, while keeping the entire request bounded
    # so the game can fall back or cancel promptly.
    await asyncio.wait_for(request(), timeout=22)
    return choice

async def main():
    request = json.loads(Path(os.environ['WUSHE_TTS_REQUEST']).read_text(encoding='utf-8-sig'))
    config = json.loads((Path(__file__).parent/'delivery.json').read_text(encoding='utf-8'))
    for job in request['jobs']:
        role = job.get('role') or ('female' if job.get('female') else 'default')
        destination = Path(job['path']+'.mp3')
        await synthesize(job['text'], role, destination, config)

if __name__ == '__main__':
    asyncio.run(main())
