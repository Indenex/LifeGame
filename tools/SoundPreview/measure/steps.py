import sys,wave,math,array
w=wave.open(sys.argv[1]);r=w.getframerate();d=array.array('h',w.readframes(w.getnframes()))
seg=float(sys.argv[2])
for k,nm in enumerate(sys.argv[3:]):
    a=int((k*seg+1)*r);b=int((k*seg+seg/2)*r);c=int((k*seg+seg/2+0.5)*r);e=int((k+1)*seg*r)
    def rms(x,y):
        v=d[2*x:2*y];return 10*math.log10(sum(t*t for t in v)/len(v)/32768**2+1e-12)
    win=int(0.05*r);best=max(sum(t*t for t in d[2*i:2*(i+win)])/(2*win) for i in range(a,b,win))
    print(f"{nm:9s} walking rms {rms(a,b):6.1f}  standing {rms(c,e):6.1f}  loudest50ms {10*math.log10(best/32768**2):6.1f}")
