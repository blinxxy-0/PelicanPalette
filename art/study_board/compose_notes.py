import sys
from PIL import Image
a=Image.open('cands/c42.png').convert('RGBA'); bare=Image.open('bare.png').convert('RGBA')
cork={(231,148,72,255),(224,135,61,255),(209,127,62,255),(228,159,94,255)}
rects={'top':(13,7,18,12),'green':(19,8,24,15),'yellow':(16,16,21,22),'land':(6,10,15,20),'bl':(6,20,12,26),'br':(17,19,25,26)}
order=['top','green','yellow','land','bl','br']
owner={}
for x in range(32):
  for y in range(32):
    if a.getpixel((x,y)) in cork: continue
    for k in order:
      x0,y0,x1,y1=rects[k]
      if x0<=x<=x1 and y0<=y<=y1: owner[(x,y)]=k;break
stages=[[],['land','top'],['land','top','br'],['land','top','br','green','bl'],order]
out=[]
for st in stages:
  im=bare.copy()
  for p,k in owner.items():
    if k in st: im.putpixel(p,a.getpixel(p))
  out.append(im)
for i,im in enumerate(out): im.save(f'stage{i}.png')
s=Image.new('RGBA',(5*170,170),(90,70,60,255))
for i,im in enumerate(out): s.alpha_composite(im.resize((160,160),Image.NEAREST),(i*170+5,5))
s.save('stages_view.png')
