import React from 'react'
import styles from './SeriesLegend.module.css'

const SeriesLegend = React.forwardRef(({series, legendClickHandler, visible}, ref) => {

    let legendIcon, legendName;

    switch ( series.type ) {
        case 'line': 
        legendIcon = 
            <svg 
                className={styles['legend-icon']} 
                height="8" 
                width="16">
                    <line 
                        x1="0" y1="4" x2="16" y2="4" 
                        stroke={series.color} strokeWidth="2">
                    </line>
            </svg>
        legendName = 
            <span 
                style={{color: `${series.color}`}}>
                {`${series.name} @ `} 
            </span>
        break
        case 'area': 
        legendIcon = 
            <svg 
                className={styles['legend-icon']} 
                height="8" width="16">
                <rect 
                    height="8" width="16" 
                    fill={`${series.colorFull}`}>
                </rect>
            </svg>
        legendName = 
            <span 
                style={{color: `${series.colorFull}`}}>
                {`${series.name} @ `} 
            </span>
        break
    }

    return (

    <div 
        ref={ref} 
        className={`${visible ? styles.active : styles.inactive}`} 
        onClick={() => {
            ref.current.classList.toggle(`${styles.inactive}`)
            legendClickHandler(series.name)
        }}
    >
        {legendIcon}
        {legendName}
        <span>{series.latestData}</span>
    </div>

    )

})

export default SeriesLegend
