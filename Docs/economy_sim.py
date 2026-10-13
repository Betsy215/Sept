"""Economy model for the 16-day chapter (12 October 2026). Mirrors the code: tip = item points x seconds left x 0.1 x
customer tip multiplier (ScoreManager); order size uniform in [min,max]; items uniform over owned foods; customers uniform
over owned customers (Kid always orders the max, Chef +1 min). A "natural path" player buys shop items in a sensible order
as soon as they can afford them, at the end of each day. Prints expected sale, tips and total per day, the stars earned
against the thresholds given, and the day each item is bought. Usage: python3 economy_sim.py [timer fraction left, default 0.4]
Edit POINTS, PRICES, DAYS and STARS below to try a rebalance."""
import sys
FRAC=float(sys.argv[1]) if len(sys.argv)>1 else 0.4          # share of the timer left when an order is served
REPLAY_PAY=0.5                                               # not used here: first run only
POINTS={"Coffee":10,"Bread":15,"Juice":20,"Apple":20,"Cake":35,"Choux":35,"Cookies":30}
PRICES={"Juice":150,"Grandma":300,"Chef":300,"CoffeeUp":350,"Choux":750,"Cake":750,"Yoga":500,"MachineUp":600,"Businessman":700,"Apple":350,"Cookies":600,
        "Vase":400,"Lantern":900,"Radio":1500}
ORDER=["Juice","Grandma","Chef","Vase","Cookies","Choux","Cake","Yoga","Lantern","Businessman","CoffeeUp","Radio","MachineUp","Apple"]   # buying order
# day: (orders, timer s, min items, max items)
DAYS={1:(7,5,1,2),2:(7,5,1,3),3:(8,5,2,3),4:(8,5,2,4),5:(9,5,2,4),6:(10,4,2,4),7:(10,4,2,4),8:(10,4,2,4),9:(11,4,2,4),10:(11,4,2,4),
      11:(11,4,2,4),12:(12,4,2,4),13:(12,4,2,4),14:(13,4,2,4),15:(13,4,2,4),16:(14,4,2,4)}
STARS_NOW={1:(100,140,190),2:(160,240,320),3:(220,330,440),4:(280,420,560),5:(420,640,850),6:(510,760,1020),7:(540,810,1080),8:(540,810,1080),
      9:(600,890,1190),10:(620,940,1250),11:(620,940,1250),12:(680,1030,1370),13:(680,1030,1370),14:(740,1110,1480),15:(740,1110,1480),16:(800,1190,1590)}   # the game's thresholds since 12 October 2026
CUST={"Girl":(1,1,0,0),"Boy":(1,1,0,0),"Kid":(1,1,0,99),"Grandma":(2,1,0,0),"Chef":(1,1,1,0),"Yoga":(1,1.5,0,0),"Businessman":(3,0.75,0,0)}  # tip x, time x, extra min, min override
DECOR={"Vase":1,"Lantern":1,"Radio":1}                        # seconds of patience each adds
def day_income(d,foods,custs,extra_s):
    orders,timer,mn,mx=DAYS[d]; avg_item=sum(POINTS[f] for f in foods)/len(foods)
    tot_sale=tot_tip=0
    for c in custs:
        tipx,timex,ext,ovr=CUST[c]
        lo=max(mn+ext,ovr) if ovr else mn+ext; lo=min(lo,mx); size=(lo+mx)/2
        sale=size*avg_item; left=(timer+extra_s)*timex*FRAC; tip=sale*left*0.1*tipx
        tot_sale+=sale; tot_tip+=tip
    n=len(custs); return orders*tot_sale/n, orders*tot_tip/n
def run(stars=STARS_NOW,verbose=True):
    foods=["Coffee","Bread"]; custs=["Girl","Boy","Kid"]; owned=set(); money=0; extra=0; bought={}; total16=0; starsum=0
    for d in range(1,17):
        sale,tip=day_income(d,foods,custs,extra); tot=sale+tip; total16+=tot
        s=sum(tot>=t for t in stars[d]); starsum+=s; money+=tot+(100 if s==3 else 0)+70   # 70 = daily treats average
        buys=[]
        for it in ORDER:
            if it in owned or money<PRICES[it]: continue
            money-=PRICES[it]; owned.add(it); buys.append(it)
            if it in POINTS: foods.append(it)
            if it in CUST: custs.append(it)
            if it in DECOR: extra+=DECOR[it]
        if verbose: print(f"day {d:2d} sale {sale:5.0f} tip {tip:4.0f} total {tot:5.0f} stars {s} {stars[d]}  money after {money:6.0f}  bought {buys}")
    if verbose: print("16-day order money",round(total16),"stars",starsum,"/48; unbought",[i for i in ORDER if i not in owned])
    return total16,starsum
if __name__=="__main__": run()
