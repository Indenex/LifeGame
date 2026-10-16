import re, html, sys, os
W = sys.argv[1]; out = sys.argv[2]
order = ["spirit","getting-started","controls","needs","crafting-and-skills","clay-and-pottery","building-and-claims","animals","other-players","tips"]
def inline(t):
    t = html.escape(t, quote=False)
    t = re.sub(r'`([^`]+)`', r'<code>\1</code>', t)
    t = re.sub(r'\*\*([^*]+)\*\*', r'<strong>\1</strong>', t)
    t = re.sub(r'\*([^*]+)\*', r'<em>\1</em>', t)
    t = re.sub(r'\[([^\]]+)\]\(([\w-]+)\.md\)', r'<a href="#\2">\1</a>', t)
    return t
KEY=re.compile(r'\b(Shift\+[A-Z]|F\d{1,2}|Esc|Enter|Space|Shift|Ctrl|[A-Z])\b')
def keycell(t):
    t=inline(t)
    t=KEY.sub(lambda m: "+".join(f"<kbd>{k}</kbd>" for k in m.group(1).split("+")), t)
    return t.replace("</kbd>+<kbd>","</kbd><span class=plus>+</span><kbd>")
def table(rows):
    rows=[[c.strip() for c in r.strip().strip("|").split("|")] for r in rows]
    rows=[r for r in rows if not all(re.fullmatch(r":?-+:?",c) for c in r)]
    h=rows[0]; o=['<div class="tbl"><table><thead><tr>'+"".join(f"<th>{inline(c)}</th>" for c in h)+"</tr></thead><tbody>"]
    for r in rows[1:]:
        o.append("<tr><td class=k>"+keycell(r[0])+"</td>"+"".join(f"<td>{inline(c)}</td>" for c in r[1:])+"</tr>")
    o.append("</tbody></table></div>"); return "\n".join(o)
def convert(md, slug):
    lines = md.splitlines(); o=[]; lst=None; para=[]; tbl=[]
    def flush():
        nonlocal para, lst
        if para: o.append("<p>"+inline(" ".join(para))+"</p>"); para=[]
        if lst: o.append(f"</{lst}>"); lst=None
    title=""
    for ln in lines:
        s=ln.strip()
        if s.startswith("|"):
            if not tbl: flush()
            tbl.append(s); continue
        if tbl: o.append(table(tbl)); tbl=[]
        if s.startswith("Next:") or s.startswith("Back to the"): continue
        if not s: flush(); continue
        m=re.match(r'^(#{1,3}) (.*)',s)
        if m:
            flush(); lvl=len(m.group(1))
            if lvl==1: title=m.group(2); continue
            o.append(f"<h3>{inline(m.group(2))}</h3>"); continue
        m=re.match(r'^(-|\d+\.) (.*)',s)
        if m:
            if para: o.append("<p>"+inline(" ".join(para))+"</p>"); para=[]
            kind="ol" if m.group(1)[0].isdigit() else "ul"
            if lst!=kind:
                if lst: o.append(f"</{lst}>")
                o.append(f"<{kind}>"); lst=kind
            o.append("<li>"+inline(m.group(2))+"</li>"); continue
        if lst: o.append(f"</{lst}>"); lst=None
        para.append(s)
    if tbl: o.append(table(tbl))
    flush()
    return title, "\n".join(o)
readme=open(os.path.join(W,"README.md")).read()
intro=[l for l in readme.splitlines() if l and not l.startswith("#") and not re.match(r'^\d+\.',l)]
lede=inline(intro[0]); note=inline(intro[1]); foot=inline(intro[-1]).replace("<em>","").replace("</em>","")
blurbs={}
for l in readme.splitlines():
    m=re.match(r'^\d+\. \[([^\]]+)\]\(([\w-]+)\.md\): (.*)',l)
    if m: blurbs[m.group(2)]=(m.group(1),m.group(3))
nav=[]; secs=[]
for i,slug in enumerate(order,1):
    t,body=convert(open(os.path.join(W,slug+".md")).read(),slug)
    short,bl=blurbs[slug]
    nav.append(f'<li><a href="#{slug}"><span class="n">{i:02d}</span>{html.escape(short)}</a></li>')
    secs.append(f'<section id="{slug}"><p class="eyebrow">Page {i} of {len(order)} · {html.escape(bl)}</p><h2>{inline(t)}</h2>{body}</section>')
tpl=open(sys.argv[3]).read()
open(out,"w").write(tpl.replace("{{LEDE}}",lede).replace("{{NOTE}}",note).replace("{{FOOT}}",foot).replace("{{NAV}}","\n".join(nav)).replace("{{SECTIONS}}","\n".join(secs)))
