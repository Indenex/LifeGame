import sys,wave,math
import array
w=wave.open(sys.argv[1]);r=w.getframerate();d=array.array('h',w.readframes(w.getnframes()))
names=sys.argv[3:];seg=float(sys.argv[2])
for k,nm in enumerate(names):
    a=int((k*seg+seg/2+1)*r);b=int((k+1)*seg*r)
    x=d[2*a:2*b]
    print(f"{nm:12s} standing rms {10*math.log10(sum(v*v for v in x)/len(x)/32768**2+1e-12):6.1f}")
