"""Talk to the Unity editor's MCP bridge (MCP for Unity, http://127.0.0.1:8080/mcp) straight over HTTP, for when the
MCP client in Claude Code did not connect at session start (13 October 2026). Streamable HTTP: initialize once,
keep the Mcp-Session-Id, then tools/call. Usage:
  python3 unity_mcp.py list
  python3 unity_mcp.py call <tool> '<json args>'
Prints the tool's text content; an image content part is saved next to this script as mcp_image_<n>.png."""
import sys, json, base64, urllib.request, os
URL="http://127.0.0.1:8080/mcp"; SESS=os.path.join(os.path.dirname(__file__),".mcp_session")
def post(body, session=None):
    req=urllib.request.Request(URL,data=json.dumps(body).encode(),headers={"Content-Type":"application/json","Accept":"application/json, text/event-stream",**({"Mcp-Session-Id":session} if session else {})})
    with urllib.request.urlopen(req,timeout=120) as r:
        sid=r.headers.get("Mcp-Session-Id"); raw=r.read().decode(); ctype=r.headers.get("Content-Type","")
    if "text/event-stream" in ctype:
        msgs=[json.loads(l[5:].strip()) for l in raw.splitlines() if l.startswith("data:") and l[5:].strip()]
        return (msgs[-1] if msgs else None), sid
    return (json.loads(raw) if raw.strip() else None), sid
def session():
    if os.path.exists(SESS):
        return open(SESS).read().strip()
    res,sid=post({"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-03-26","capabilities":{},"clientInfo":{"name":"claude-code-curl","version":"1"}}})
    if sid: open(SESS,"w").write(sid)
    post({"jsonrpc":"2.0","method":"notifications/initialized"},sid)
    return sid
def call(tool,args):
    sid=session()
    res,_=post({"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":tool,"arguments":args}},sid)
    if res is None: return "(no response)"
    if "error" in res: return "ERROR "+json.dumps(res["error"])
    out=[]; n=0
    for part in res.get("result",{}).get("content",[]):
        if part.get("type")=="text": out.append(part["text"])
        elif part.get("type")=="image":
            n+=1; p=os.path.join(os.path.dirname(__file__),f"mcp_image_{n}.png"); open(p,"wb").write(base64.b64decode(part["data"])); out.append("[image saved "+p+"]")
    return "\n".join(out)
if __name__=="__main__":
    if sys.argv[1]=="list":
        sid=session(); res,_=post({"jsonrpc":"2.0","id":3,"method":"tools/list","params":{}},sid)
        print("\n".join(t["name"]+": "+t.get("description","")[:90] for t in res["result"]["tools"]))
    elif sys.argv[1]=="exec":      # python3 unity_mcp.py exec <file.cs>: runs the file as a method body in the editor
        print(call("execute_code",{"action":"execute","code":open(sys.argv[2]).read()}))
    elif sys.argv[1]=="shot":      # python3 unity_mcp.py shot <name>: Game view capture to Captures/<name>.png
        print(call("manage_camera",{"action":"screenshot","screenshot_file_name":sys.argv[2],"properties":{"include_image":False}}))
    elif sys.argv[1]=="reset":
        if os.path.exists(SESS): os.remove(SESS); print("session cleared")
    else:
        print(call(sys.argv[2], json.loads(sys.argv[3]) if len(sys.argv)>3 else {}))
