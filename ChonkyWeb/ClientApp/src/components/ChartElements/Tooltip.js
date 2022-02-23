import React from 'react'
import styles from './Tooltip.module.css'
import TooltipBits from './TooltipBits'

const Tooltip = React.forwardRef(({symbol, desc, series}, {tooltipRef, dateRef}) => {

    return (

        <div ref={tooltipRef} className={styles['floating-tooltip']}>
        <div className={styles['symbol']}>{symbol}</div>
        <div>{desc}</div>
        {Object.keys(series).map((sName) => (
            <TooltipBits 
                key={sName}
                series={series[sName]} 
                ref={series[sName].ref} 
            />
        ))}
        <div ref={dateRef}></div>
      </div>

    )
})


export default Tooltip