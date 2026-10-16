import sys,wave,struct,math
w=wave.open(sys.argv[1]);n=w.getnframes();r=w.getframerate()
d=struct.unpack('<%dh'%(n*2),w.readframes(n))
segs=[float(x) for x in sys.argv[2:]]
pos=0
for sec in segs:
    a=int(pos*r);b=int((pos+sec-1.2)*r);pos+=sec  # skip last ~1s (ending)
    a+=int(2*r)
    win=int(0.05*r);best=0;tot=0;pk=0
    for i in range(a,b,win):
        p=sum((d[2*j]/32768)**2+(d[2*j+1]/32768)**2 for j in range(i,min(i+win,b)))/(2*win)
        best=max(best,p);tot+=p
        pk=max(pk,max(abs(d[2*j]) for j in range(i,min(i+win,b)))/32768)
    cnt=len(range(a,b,win))
    print(f"seg{segs.index(sec)} {sec}s rms {10*math.log10(tot/cnt+1e-12):6.1f}  max50ms {10*math.log10(best+1e-12):6.1f}  peak {20*math.log10(pk+1e-9):6.1f}")
