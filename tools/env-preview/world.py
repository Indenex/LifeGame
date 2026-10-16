# Python copy of the value noise in shared/World.cs, for env preview scripts.
import math
M=0xFFFFFFFF
def H(x,z,s):
    h=((x*0x8DA6B343)&M)^((z*0xD8163841)&M)^((s*0xCB1AB31F)&M)
    h^=h>>13; h=(h*0x5BD1E995)&M; h^=h>>15; return h
def R(x,z,s): return (H(x,z,s)&0xFFFFFF)/16777216
def sm(t): return t*t*(3-2*t)
def VN(x,z,sc,s):
    fx,fz=x/sc,z/sc; x0,z0=math.floor(fx),math.floor(fz); tx,tz=sm(fx-x0),sm(fz-z0)
    a,b,c,d=R(x0,z0,s),R(x0+1,z0,s),R(x0,z0+1,s),R(x0+1,z0+1,s)
    t=a+(b-a)*tx; bo=c+(d-c)*tx; return t+(bo-t)*tz
def BH(x,z): return VN(x,z,32,1)*3+VN(x,z,8,2)*0.6-1.5
def clay(x,z): return VN(x,z,10,3)+(0.15 if BH(x,z)<-0.4 else 0)
