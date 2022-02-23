import React from 'react'
import styles from './RealTimeButton.module.css'

const RealTimeButton = React.forwardRef((props, ref) => {

    return (
        <div 
            ref={ref}
            className={`${styles['real-time-button']}`}
            style={{
                left: (props.containerWidth - 36 - 120) + 'px', // button width = 36px
                top: (props.containerHeight - 36 - 30) + 'px', // button height = 36px
                color: '#4c525e',
                display: props.btnVisible,
            }}
            onClick={props.btnClickHandler}
        >
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 14 14" width="14" height="14"><path fill="none" stroke="currentColor" strokeLinecap="round" strokeWidth="2" d="M6.5 1.5l5 5.5-5 5.5M3 4l2.5 3L3 10"></path></svg>
        </div>
    )
})

export default RealTimeButton