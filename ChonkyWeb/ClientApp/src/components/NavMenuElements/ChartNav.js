import React from 'react';
import { Dropdown, DropdownItem } from '@duik/it';
import { Link } from 'react-router-dom';
import DayPickerInput from 'react-day-picker/DayPickerInput'
import { getLastDate } from '../utils/tradingDayUtil'
import 'react-day-picker/lib/style.css';

const ChartNav = ({activeTicker, setActiveTicker, tickers, currDate, handleDateChange}) => (
    <>
        <Dropdown 
            buttonText={activeTicker}
            buttonProps={{transparent: true, style: {height: '2rem'}}}>
            {tickers.map(ticker => {
            return <DropdownItem 
                key={ticker} 
                Component={Link} 
                to={`/graph?ticker=${ticker}&date=${encodeURIComponent(currDate)}`} 
                onClick={() => setActiveTicker(ticker)}
                >{ticker}
                </DropdownItem>
            })}
        </Dropdown>
        <DayPickerInput 
            value={new Date(decodeURIComponent(currDate))}
            initialMonth={new Date()} 
            dayPickerProps={{
                disabledDays: [{
                    daysOfWeek: [0,6],
                    },{
                        after: new Date(),
                        before: getLastDate(new Date())
                    }]
                }}
            placeholder='MM/DD/YYYY'
            formatDate={date => date.toLocaleDateString()}
            onDayChange={handleDateChange}
            className="date-picker"
        />
    </>
)

export default ChartNav





