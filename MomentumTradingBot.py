import numpy as np

def slope(rsiarray, steps, upward):
        counter = 1
        for index in range(steps-1):
            if upward:
                if rsiarray[index] >= rsiarray[index+1]:
                    counter += 1
            else:
                if rsiarray[index] <= rsiarray[index+1]:
                    counter += 1
        return(counter == steps)

def initialize(state):
    state.lim_price = 0.0
    state.buysig = False
    state.sellsig = False
    state.order_count = 1
    state.tral_order = None
    state.trail_per = 0.02
    state.sell_amount = 0
    state.tral_buy = None
    state.fall_list = []
    state.temp_list = []
    state.vol_list = []

@schedule(interval="15m", symbol="BTCUSDT")
def handler15(state, data):
    # variables

    state.buysig = False
    state.sellsig = False
    chandperiod = 30
    bear_bull_period = 6
    # Predict global trend

    median = data.close.super_smoother(200).last[0]
    contour = data.close.ema(5)[-1:-145:-1][0]
    lowprice = np.amin(contour)
    highprice = np.amax(contour)
    low_ind = np.argmin(contour)
    high_ind = np.argmax(contour)
    rsi = data.rsi(50).ema(13)[-1:-3:-1][0]

    if highprice > median*1.02 and lowprice < median*0.985:
        # high volatility

        marker = 2
        chandmul = 3
        sell_ind = -10
        buy_ind = 10
        strat_percent = 1.02
        buy_length = 1
        tral_percent = 0.03
        qst_cut = -5
        qst_up = 0
        slope_buy = 7
        stop_loss_per = 0.97

    elif low_ind < high_ind and contour[0] < median*1.02:
        # downtrend

        marker = -1
        chandmul = 3.236
        sell_ind = -5
        buy_ind = 10
        strat_percent = 1.01
        buy_length = 1
        tral_percent = 0.04
        qst_cut = 0
        qst_up = 4
        slope_buy = 11
        stop_loss_per = 0.97
        
    elif low_ind > high_ind and contour[0] > median*1.02:
        # uptrend

        marker = 1
        chandmul = 2.8
        sell_ind = -15
        buy_ind = 15
        strat_percent = 1.01
        buy_length = 4
        tral_percent = 0.035
        qst_up = 0
        slope_buy = 9
        stop_loss_per = 0.97
        if rsi[0] >= 50:
            qst_cut = 2
        else:
            qst_cut = -2
        
    else:
        # stable

        marker = 0
        chandmul = 3.236
        sell_ind = -10
        buy_ind = 5
        buy_length = 1
        tral_percent = 0.04
        qst_up = 0
        slope_buy = 12
        stop_loss_per = 0.97
        if rsi[0] >= 50:
            strat_percent = 1.024
            qst_cut = 4
        else:
            strat_percent = 1.012
            qst_cut = -1

    if contour[-1] > contour[80] > contour[40] > contour[0] and contour[-1] > contour[0]*1.028:
        point = 1
    else:
        point = 0

    state.fall_list.append(point)

    if len(state.fall_list) > 35:
        state.fall_list.pop(0)

    if np.amax(state.fall_list) == 1:
        # High risk of fall
        chandmul = 2.5
        sell_ind = -10
        buy_ind = 10
        strat_percent = 1.022
        buy_length = 1
        tral_percent = 0.045
        qst_cut = 0
        qst_up = 5
        slope_buy = 7
        stop_loss_per = 0.97
    
    if data.close.ema(15).last[0] < 14000:
        chandmul *= 0.6
        sell_ind *= 4
        buy_ind *= 4
    
    bbands = data.bbands(15, 2)
    bb_up = bbands['bbands_upper'].last[0]
    bb_low = bbands['bbands_lower'].last[0]
    nsr = round(data.close.last[0]/((bb_up+bb_low)/2), 2)
    state.temp_list.append(nsr)

    if len(state.temp_list) > 80: state.temp_list.pop(0)

    vals_greater = (np.array(state.temp_list) > 1.02).sum()
    vals_lower = (np.array(state.temp_list) < 0.98).sum()
    dot = 0
    if abs(vals_greater - vals_lower) < 15 and (vals_greater + vals_lower) > 5: dot = 1
    
    state.vol_list.append(dot)

    if len(state.vol_list) > 60: state.vol_list.pop(0)

    if np.amax(state.vol_list) == 1: sell_ind *= 4

    rocr1 = (np.subtract(data.close.ema(4).rocr(1)[-1:-21:-1][0], 1)) * 100 # The main data
    volume = data.volume[-1:-16:-1][0] # Volume
    price = data.close[-1:-4:-1][0]
    price_open = data.open[-1:-3:-1][0]
    low_price_last = data.low.last[0]
    high_price_prev = data.high[-2:-3:-1][0]
    trendfactor = data.open.ma(3, m_a_type=3)[-1:-2:-1][0]
    qst = data.qstick(50).ema(2)[-1:-10:-1][0] # Determines when to buy
    ind = data.bull_bear_power(period=bear_bull_period)['power'][-1:-20:-1][0] # local trend finding

    with PlotVisibility.hidden():
        chandelier = data.chandelier(period=chandperiod, multiplier=chandmul)['trend'][-1:-16:-1][0]
    # Calculating acceleration and other params

    if state.trail_per < ((high_price_prev[0] / low_price_last) - 1):
        state.trail_per = round(((high_price_prev[0] / low_price_last) - 1) * 1.01, 3)
        if state.trail_per > tral_percent:
            state.trail_per = tral_percent
    
    length = 20
    accel = np.subtract(rocr1[0:(length-1)], rocr1[1:length])
    accelchange = np.subtract(accel[0:(length-2)], accel[1:(length-1)])

    if data.close.last[0] < trendfactor:
        downtrend = True
    else:
        downtrend = False
    # Fetch portfolio

    portfolio = query_portfolio()
    balance_quoted = portfolio.excess_liquidity_quoted
    position = query_open_position_by_symbol(data.symbol, include_dust=False)
    has_position = position is not None

    if not has_position:
        state.order_count = 1
        state.tral_order = None
        state.sell_amount = 0

    buy_value = (float(balance_quoted)*0.999)
    buy_amount = (buy_value / price[0]) * 0.5**(buy_length-state.order_count)
    # Finding peaks

    if accel[0] > 0 and accel[1] < 0:

        if chandelier[0] < 0 and slope(chandelier, slope_buy, True) and ind[0] > buy_ind and qst[0] < qst_cut and not downtrend:

            state.buysig = True
    if low_ind < high_ind and lowprice*strat_percent < highprice:

        if chandelier[0] > 0 and ind[0] < sell_ind and accel[0] - accel[1] < 0 and qst[0] >= qst_up:
            state.sellsig = True
    else:

        if chandelier[0] > 0 and ind[0] < sell_ind and accel[0] > 0 and accel[1] < 0:
            state.sellsig = True
    # Buying and selling

    if state.buysig and state.order_count <= buy_length and price[0] > price[1]:
        if np.amin(volume) < volume[0] < np.amax(volume):

            if state.tral_buy is not None:
                cancel_order(state.tral_buy.id)
                state.tral_buy = None

            if state.tral_order is not None:
                cancel_order(state.tral_order.id)

            if buy_amount * price[0] > 10:
                order = order_market_amount(symbol=data.symbol, amount=buy_amount)
                state.sell_amount -= subtract_order_fees(order.quantity)
                state.lim_price = data.close.last[0]
                state.order_count += 1
                state.tral_order = order_trailing_iftouched_amount(data.symbol, amount=state.sell_amount, trailing_percent=state.trail_per, stop_price=state.lim_price * stop_loss_per)


    if state.sellsig and has_position and price[0] < price[1]:

        if state.tral_buy is not None:
                cancel_order(state.tral_buy.id)
                state.tral_buy = None

        if state.tral_order is not None:
            cancel_order(state.tral_order.id)
        close_position(symbol="BTCUSDT")